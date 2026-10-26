using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Snackbox.Api.Dtos;

namespace Snackbox.Api.Services;

/// <summary>
/// The Outlook.com / Microsoft 365 mailbox PayPal's "payment received" mails arrive in - for a
/// private PayPal account, which gets no API access. Read through Microsoft Graph with read-only
/// <c>Mail.Read</c>, signed in like the OneDrive backup (device code flow against the admin's own
/// app registration, which may be the same one); the refresh token is stored with Data Protection
/// in %ProgramData%\Snackbox\paypal-mailbox.json.
///
/// Only mails from PayPal in the inbox count, and only when the receiving server confirmed they
/// really come from PayPal (DMARC pass in its Authentication-Results) - anyone can write a mail
/// that looks like a payment, and a booked one lowers somebody's debt.
/// </summary>
// ponytail: the device code sign-in repeats OneDriveBackup's; share it once a third Graph feature comes
public partial class PayPalMailbox(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    IDataProtectionProvider dataProtection,
    ILogger<PayPalMailbox> logger)
{
    private const string Graph = "https://graph.microsoft.com/v1.0";
    private const string Scopes = "Mail.Read User.Read offline_access";

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static PendingSignIn? _pending;

    private readonly IDataProtector _protector = dataProtection.CreateProtector("Snackbox.PayPalMailbox");

    private record PendingSignIn(string ClientId, string DeviceCode, DateTime ExpiresAt);

    private string StatePath => configuration["PayPal:Mailbox:StateFile"] ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Snackbox", "paypal-mailbox.json");

    private string Authority => $"https://login.microsoftonline.com/{configuration["PayPal:Mailbox:Tenant"] ?? "common"}/oauth2/v2.0";

    private class State
    {
        public string? ClientId { get; set; }
        public string? Account { get; set; }
        public string? ProtectedRefreshToken { get; set; }
    }

    public bool Connected => Load() is { ProtectedRefreshToken: not null, ClientId: not null };

    public PayPalMailboxStatusDto GetStatus()
    {
        var state = Load();
        return new PayPalMailboxStatusDto { ClientId = state.ClientId, Connected = state.ProtectedRefreshToken != null, Account = state.Account };
    }

    public async Task SetClientIdAsync(string clientId)
    {
        clientId = clientId.Trim();
        if (!Guid.TryParse(clientId, out _)) throw new ArgumentException("The client id is a GUID (Application (client) ID of the app registration).");
        await Gate.WaitAsync();
        try
        {
            var state = Load();
            if (state.ClientId == clientId) return;
            // A token belongs to the app registration it was issued for
            Save(new State { ClientId = clientId });
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task<OneDriveSignInDto> StartSignInAsync()
    {
        var clientId = Load().ClientId ?? throw new InvalidOperationException("Enter the client id of your app registration first.");

        using var http = httpClientFactory.CreateClient();
        using var response = await http.PostAsync($"{Authority}/devicecode",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["client_id"] = clientId, ["scope"] = Scopes }));
        var body = await response.Content.ReadFromJsonAsync<DeviceCodeResponse>();
        if (!response.IsSuccessStatusCode || body?.DeviceCode == null)
            throw new InvalidOperationException($"Microsoft refused the sign-in: {body?.ErrorDescription ?? response.StatusCode.ToString()}");

        _pending = new PendingSignIn(clientId, body.DeviceCode, DateTime.UtcNow.AddSeconds(body.ExpiresIn));
        return new OneDriveSignInDto
        {
            UserCode = body.UserCode!,
            VerificationUri = body.VerificationUri ?? "https://microsoft.com/devicelogin",
            ExpiresInSeconds = body.ExpiresIn,
            IntervalSeconds = Math.Max(body.Interval, 5)
        };
    }

    /// <summary>One poll: "pending" until the admin finished signing in, then "connected".</summary>
    public async Task<string> PollSignInAsync()
    {
        var pending = _pending;
        if (pending == null || pending.ExpiresAt < DateTime.UtcNow) return "expired";

        using var http = httpClientFactory.CreateClient();
        using var response = await http.PostAsync($"{Authority}/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
            ["client_id"] = pending.ClientId,
            ["device_code"] = pending.DeviceCode
        }));
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();

        if (token?.Error is "authorization_pending" or "slow_down") return "pending";
        _pending = null;
        if (token?.RefreshToken == null || token.AccessToken == null)
            return $"failed: {token?.ErrorDescription ?? token?.Error ?? response.StatusCode.ToString()}";

        var account = await GetAccountAsync(token.AccessToken);
        await Gate.WaitAsync();
        try
        {
            var state = Load();
            state.ProtectedRefreshToken = _protector.Protect(token.RefreshToken);
            state.Account = account;
            Save(state);
        }
        finally
        {
            Gate.Release();
        }

        logger.LogInformation("PayPal mailbox connected: {Account}", account);
        return "connected";
    }

    public async Task DisconnectAsync()
    {
        await Gate.WaitAsync();
        try
        {
            var state = Load();
            state.ProtectedRefreshToken = null;
            state.Account = null;
            Save(state);
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <summary>The payments in genuine PayPal mails that arrived in the inbox since <paramref name="from"/>.</summary>
    public async Task<List<PayPalApi.Received>> ReceivedAsync(DateTime from, CancellationToken cancellationToken)
    {
        var state = Load();
        if (state.ProtectedRefreshToken == null || state.ClientId == null) return new();

        using var http = httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await RefreshAsync(state));

        var result = new List<PayPalApi.Received>();
        string? next = $"{Graph}/me/mailFolders/inbox/messages?$filter=receivedDateTime ge {from.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}" +
                       "&$select=id,from,receivedDateTime&$top=100";
        while (next != null)
        {
            using var response = await http.GetAsync(next, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new PayPalException($"Reading the mailbox failed ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync(cancellationToken)}");
            var page = await response.Content.ReadFromJsonAsync<MessagePage>(cancellationToken);
            foreach (var message in page?.Value ?? [])
            {
                if (!FromPayPal(message.From?.EmailAddress?.Address)) continue;

                using var full = await http.GetAsync($"{Graph}/me/messages/{message.Id}?$select=body,internetMessageHeaders,receivedDateTime", cancellationToken);
                if (!full.IsSuccessStatusCode) continue;
                var detail = await full.Content.ReadFromJsonAsync<Message>(cancellationToken);
                if (detail?.Body?.Content == null) continue;

                if (!IsGenuine(detail.InternetMessageHeaders))
                {
                    logger.LogWarning("PayPal mailbox: ignored a mail from {From} that did not pass DMARC", message.From?.EmailAddress?.Address);
                    continue;
                }

                if (PayPalMail.Parse(detail.Body.Content, (detail.ReceivedDateTime ?? message.ReceivedDateTime ?? DateTimeOffset.UtcNow).UtcDateTime) is { } received)
                    result.Add(received);
            }
            next = page?.NextLink;
        }
        return result;
    }

    /// <summary>PayPal's own addresses (service@paypal.de, service@intl.paypal.com, ...), no look-alikes.</summary>
    public static bool FromPayPal(string? address) =>
        address != null && PayPalAddress().IsMatch(address);

    /// <summary>
    /// The receiving server's verdict - the topmost Authentication-Results header, the one Outlook
    /// added itself (lower ones could come from the sender): DMARC passed for a PayPal domain.
    /// </summary>
    public static bool IsGenuine(IEnumerable<MessageHeader>? headers)
    {
        var results = headers?.FirstOrDefault(h => string.Equals(h.Name, "Authentication-Results", StringComparison.OrdinalIgnoreCase))?.Value;
        return results != null && DmarcPassForPayPal().IsMatch(results);
    }

    [GeneratedRegex(@"(?i)^[\w.+-]+@(?:[\w-]+\.)*paypal\.(?:de|com)$")]
    private static partial Regex PayPalAddress();

    [GeneratedRegex(@"(?i)\bdmarc=pass\b[^;]*\bheader\.from=(?:[\w-]+\.)*paypal\.(?:de|com)\b")]
    private static partial Regex DmarcPassForPayPal();

    private async Task<string> RefreshAsync(State state)
    {
        using var http = httpClientFactory.CreateClient();
        using var response = await http.PostAsync($"{Authority}/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = state.ClientId!,
            ["refresh_token"] = _protector.Unprotect(state.ProtectedRefreshToken!),
            ["scope"] = Scopes
        }));
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        if (token?.AccessToken == null)
            throw new PayPalException($"The mailbox sign-in is no longer valid - connect again ({token?.ErrorDescription ?? token?.Error ?? response.StatusCode.ToString()})");

        if (token.RefreshToken != null)
        {
            await Gate.WaitAsync();
            try
            {
                var latest = Load();
                latest.ProtectedRefreshToken = _protector.Protect(token.RefreshToken);
                Save(latest);
            }
            finally
            {
                Gate.Release();
            }
        }
        return token.AccessToken;
    }

    private async Task<string> GetAccountAsync(string accessToken)
    {
        using var http = httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await http.GetAsync($"{Graph}/me?$select=mail,userPrincipalName,displayName");
        var me = response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<Me>() : null;
        return me?.Mail ?? me?.UserPrincipalName ?? me?.DisplayName ?? "Outlook";
    }

    private State Load()
    {
        try
        {
            return File.Exists(StatePath) ? JsonSerializer.Deserialize<State>(File.ReadAllText(StatePath)) ?? new State() : new State();
        }
        catch (JsonException)
        {
            return new State();
        }
    }

    private void Save(State state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
        File.WriteAllText(StatePath, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
    }

    // --- wire formats ---------------------------------------------------------------------

    private class DeviceCodeResponse
    {
        [JsonPropertyName("device_code")] public string? DeviceCode { get; set; }
        [JsonPropertyName("user_code")] public string? UserCode { get; set; }
        [JsonPropertyName("verification_uri")] public string? VerificationUri { get; set; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("interval")] public int Interval { get; set; }
        [JsonPropertyName("error_description")] public string? ErrorDescription { get; set; }
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
        [JsonPropertyName("error")] public string? Error { get; set; }
        [JsonPropertyName("error_description")] public string? ErrorDescription { get; set; }
    }

    private class Me
    {
        [JsonPropertyName("mail")] public string? Mail { get; set; }
        [JsonPropertyName("userPrincipalName")] public string? UserPrincipalName { get; set; }
        [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
    }

    private class MessagePage
    {
        [JsonPropertyName("value")] public List<Message>? Value { get; set; }
        [JsonPropertyName("@odata.nextLink")] public string? NextLink { get; set; }
    }

    private class Message
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("receivedDateTime")] public DateTimeOffset? ReceivedDateTime { get; set; }
        [JsonPropertyName("from")] public Recipient? From { get; set; }
        [JsonPropertyName("body")] public ItemBody? Body { get; set; }
        [JsonPropertyName("internetMessageHeaders")] public List<MessageHeader>? InternetMessageHeaders { get; set; }
    }

    private class Recipient
    {
        [JsonPropertyName("emailAddress")] public EmailAddress? EmailAddress { get; set; }
    }

    private class EmailAddress
    {
        [JsonPropertyName("address")] public string? Address { get; set; }
    }

    private class ItemBody
    {
        [JsonPropertyName("content")] public string? Content { get; set; }
    }

    public class MessageHeader
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
    }
}
