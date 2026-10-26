using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Snackbox.Api.Services;

/// <summary>
/// Reads payments received on a PayPal account through PayPal's Transaction Search API
/// (<c>GET /v1/reporting/transactions</c>), authenticated with the client id and secret of a
/// REST app on developer.paypal.com whose "Transaction search" feature is switched on. PayPal
/// lists a transaction up to 3 hours after it happened and answers at most 31 days per request.
/// </summary>
public class PayPalApi(HttpClient http)
{
    public const string LiveUrl = "https://api-m.paypal.com";
    public const string SandboxUrl = "https://api-m.sandbox.paypal.com";

    private static readonly TimeSpan MaxRange = TimeSpan.FromDays(31);

    /// <param name="ReferenceId">For money going out: the transaction it takes back (reversal, chargeback, refund).</param>
    public record Received(string TransactionId, DateTime At, decimal Amount, string Currency,
                           string PayerKey, string? PayerName, string? PayerEmail, string? Note, string? ReferenceId = null);

    /// <summary>
    /// Completed incoming payments between <paramref name="from"/> and <paramref name="to"/>, and
    /// the completed outgoing ones that take an earlier payment back (negative amount, ReferenceId).
    /// </summary>
    public async Task<List<Received>> ReceivedAsync(string baseUrl, string clientId, string secret,
                                                    DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var token = await TokenAsync(baseUrl, clientId, secret, cancellationToken);
        var result = new List<Received>();

        for (var start = from; start < to; start += MaxRange)
        {
            var end = start + MaxRange < to ? start + MaxRange : to;
            for (var page = 1; ; page++)
            {
                var url = $"{baseUrl}/v1/reporting/transactions?start_date={Iso(start)}&end_date={Iso(end)}" +
                          $"&fields=transaction_info,payer_info&page_size=500&page={page}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var response = await http.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw new PayPalException($"PayPal answered {(int)response.StatusCode}: {Shorten(body)}");

                using var json = JsonDocument.Parse(body);
                result.AddRange(Parse(json.RootElement));

                var totalPages = json.RootElement.TryGetProperty("total_pages", out var pages) ? pages.GetInt32() : 1;
                if (page >= totalPages) break;
            }
        }
        return result;
    }

    /// <summary>The incoming, completed payments of one page of PayPal's answer.</summary>
    public static IEnumerable<Received> Parse(JsonElement root)
    {
        if (!root.TryGetProperty("transaction_details", out var details)) yield break;

        foreach (var detail in details.EnumerateArray())
        {
            var info = detail.GetProperty("transaction_info");
            // S = completed; pending, denied and reversed ones are no money (yet)
            if (Text(info, "transaction_status") != "S") continue;
            if (!info.TryGetProperty("transaction_amount", out var money)) continue;
            var amount = decimal.Parse(Text(money, "value") ?? "0", CultureInfo.InvariantCulture);
            var referenceId = Text(info, "paypal_reference_id");
            if (amount == 0 || (amount < 0 && referenceId == null)) continue; // fees, money we sent

            detail.TryGetProperty("payer_info", out var payer);
            var email = payer.ValueKind == JsonValueKind.Object ? Text(payer, "email_address") : null;
            var accountId = payer.ValueKind == JsonValueKind.Object ? Text(payer, "account_id") : null;
            var key = accountId ?? email ?? (amount < 0 ? "" : null);
            if (key == null) continue; // nobody to assign it to

            string? name = null;
            if (payer.ValueKind == JsonValueKind.Object && payer.TryGetProperty("payer_name", out var payerName))
            {
                name = Text(payerName, "alternate_full_name")
                       ?? string.Join(' ', new[] { Text(payerName, "given_name"), Text(payerName, "surname") }.Where(n => n != null));
            }

            var note = string.Join(" - ", new[] { Text(info, "transaction_subject"), Text(info, "transaction_note") }
                                       .Where(n => !string.IsNullOrWhiteSpace(n)).Distinct());

            yield return new Received(
                Text(info, "transaction_id")!,
                DateTime.Parse(Text(info, "transaction_initiation_date")!, CultureInfo.InvariantCulture,
                               DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
                amount,
                Text(money, "currency_code") ?? "EUR",
                key,
                string.IsNullOrWhiteSpace(name) ? null : name,
                email,
                note.Length == 0 ? null : note,
                amount < 0 ? referenceId : null);
        }
    }

    private async Task<string> TokenAsync(string baseUrl, string clientId, string secret, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/oauth2/token")
        {
            Content = new FormUrlEncodedContent([new KeyValuePair<string, string>("grant_type", "client_credentials")])
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{secret}")));
        using var response = await http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new PayPalException($"PayPal refused the client id / secret ({(int)response.StatusCode}): {Shorten(body)}");

        using var json = JsonDocument.Parse(body);
        return json.RootElement.GetProperty("access_token").GetString()!;
    }

    private static string? Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string Iso(DateTime value) => value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    private static string Shorten(string body) => body.Length > 300 ? body[..300] : body;
}

public class PayPalException(string message) : Exception(message);
