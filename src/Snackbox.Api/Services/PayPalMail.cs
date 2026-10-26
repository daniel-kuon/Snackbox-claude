using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace Snackbox.Api.Services;

/// <summary>
/// Reads PayPal's German "Du hast eine Zahlung erhalten" e-mail - the only record of a payment
/// a private PayPal account gets (PayPal's API is for business accounts only). The mail names
/// the payer, but not their e-mail address, so the payer is identified by name.
/// </summary>
public static partial class PayPalMail
{
    /// <summary>The payment in a PayPal mail, or null when it is no "payment received" mail.</summary>
    public static PayPalApi.Received? Parse(string html, DateTime receivedAt)
    {
        var text = ToText(html);

        // The note is the payer's own text: code, amount and payer come only from PayPal's part
        // above it, and each label exactly once - a note cannot pass for any of them
        var noteStart = NoteLabel().Match(text);
        var head = noteStart.Success ? text[..noteStart.Index] : text;
        var id = TransactionId().Matches(head);
        var amount = Amount().Matches(head);
        var payer = Payer().Matches(head);
        if (id.Count != 1 || amount.Count != 1 || payer.Count != 1) return null;

        var value = decimal.Parse(amount[0].Groups["value"].Value, NumberStyles.Number, CultureInfo.GetCultureInfo("de-DE"));
        var note = Note().Match(text);
        var name = payer[0].Groups["name"].Value.Trim();

        return new PayPalApi.Received(
            id[0].Groups["id"].Value,
            receivedAt,
            value,
            amount[0].Groups["currency"].Value,
            PayerKey(name),
            name,
            null,
            note.Success ? note.Groups["note"].Value.Trim() : null);
    }

    /// <summary>Mails name the payer but give no PayPal account id or e-mail.</summary>
    public static string PayerKey(string payerName) => "name:" + payerName.Trim().ToLowerInvariant();

    /// <summary>One line per paragraph, cell or line break - how the labels and values are laid out.</summary>
    public static string ToText(string html)
    {
        var s = ScriptsAndStyles().Replace(html, "");
        s = LineBreaks().Replace(s, "\n");
        s = WebUtility.HtmlDecode(Tags().Replace(s, ""));
        return string.Join('\n', s.Split('\n').Select(l => l.Replace(' ', ' ').Trim()).Where(l => l.Length > 0));
    }

    [GeneratedRegex(@"(?is)<(script|style|head)\b.*?</\1>")]
    private static partial Regex ScriptsAndStyles();

    [GeneratedRegex(@"(?i)<br\s*/?>|</(p|div|tr|td|h\d|table|span|li)>")]
    private static partial Regex LineBreaks();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex Tags();

    [GeneratedRegex(@"(?m)^Transaktionscode\n(?<id>[A-Z0-9]{10,25})$")]
    private static partial Regex TransactionId();

    [GeneratedRegex(@"(?m)^Erhaltener Betrag\n(?<value>[\d.]+,\d{2})\s*€?\s*(?<currency>[A-Z]{3})$")]
    private static partial Regex Amount();

    [GeneratedRegex(@"(?m)^(?<name>.+?) hat dir [\d.]+,\d{2}\s*€?\s*[A-Z]{3} gesendet$")]
    private static partial Regex Payer();

    [GeneratedRegex(@"(?m)^Mitteilung von ")]
    private static partial Regex NoteLabel();

    // The note: the lines after "Mitteilung von <payer>" up to PayPal's own text below it
    [GeneratedRegex(@"(?ms)^Mitteilung von [^\n]+\n(?<note>.+?)(?=\n(Du siehst das Geld|Bist du zufrieden|Hilfe & Kontakt)|\z)")]
    private static partial Regex Note();
}
