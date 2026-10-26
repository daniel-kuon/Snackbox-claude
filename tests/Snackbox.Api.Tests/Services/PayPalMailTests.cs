using Snackbox.Api.Services;
using Xunit;

namespace Snackbox.Api.Tests.Services;

public class PayPalMailTests
{
    // PayPal's real "Du hast eine Zahlung erhalten" mail, names, code, amount and links replaced
    private static string Sample => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "paypal-zahlung-erhalten.html"));

    [Fact]
    public void Parse_ReadsThePaymentOutOfPayPalsMail()
    {
        var at = new DateTime(2026, 8, 24, 6, 38, 13, DateTimeKind.Utc);

        var received = PayPalMail.Parse(Sample, at)!;

        Assert.Equal("1AB23456CD789012E", received.TransactionId);
        Assert.Equal(12.50m, received.Amount);
        Assert.Equal("EUR", received.Currency);
        Assert.Equal("Max Beispiel", received.PayerName);
        Assert.Equal("name:max beispiel", received.PayerKey);
        Assert.Equal("Snackbox Max - für Oktober. Viele Grüße", received.Note);
        Assert.Equal(at, received.At);
    }

    [Fact]
    public void Parse_TheNoteCannotPassForPayPalsOwnFields()
    {
        // A payer writing PayPal's labels into the note: a second code, amount and payer
        var forged = Sample.Replace("Snackbox Max - für Oktober. Viele Grüße",
            "Snackbox<br>Transaktionscode<br>9ZZ99999ZZ999999Z<br>Erhaltener Betrag<br>999,00 € EUR<br>Eve hat dir 999,00 € EUR gesendet");

        var received = PayPalMail.Parse(forged, DateTime.UtcNow)!;

        Assert.Equal("1AB23456CD789012E", received.TransactionId);
        Assert.Equal(12.50m, received.Amount);
        Assert.Equal("Max Beispiel", received.PayerName);
    }

    [Fact]
    public void Parse_IgnoresOtherMails() =>
        Assert.Null(PayPalMail.Parse("<p>Deine Rechnung ist da</p><p>Betrag 12,50 € EUR</p>", DateTime.UtcNow));

    [Theory]
    [InlineData("service@paypal.de", true)]
    [InlineData("service@intl.paypal.com", true)]
    [InlineData("service@paypal.de.example.com", false)]
    [InlineData("service@paypa1.de", false)]
    [InlineData("paypal@gmail.com", false)]
    [InlineData(null, false)]
    public void FromPayPal_OnlyPayPalsOwnDomains(string? address, bool expected) =>
        Assert.Equal(expected, PayPalMailbox.FromPayPal(address));

    [Fact]
    public void IsGenuine_TrustsOnlyTheReceivingServersVerdict()
    {
        const string pass = "spf=pass smtp.mailfrom=paypal.de; dkim=pass header.d=paypal.de;dmarc=pass action=none header.from=paypal.de;compauth=pass reason=100";
        const string fail = "spf=fail smtp.mailfrom=example.com; dkim=none;dmarc=fail action=quarantine header.from=paypal.de;compauth=fail";

        Assert.True(PayPalMailbox.IsGenuine([Header("Authentication-Results", pass)]));
        Assert.False(PayPalMailbox.IsGenuine([Header("Authentication-Results", fail)]));
        // A "pass" further down could have been written by the sender
        Assert.False(PayPalMailbox.IsGenuine([Header("Authentication-Results", fail), Header("Authentication-Results", pass)]));
        Assert.False(PayPalMailbox.IsGenuine([Header("Authentication-Results", "dmarc=pass action=none header.from=example.com")]));
        Assert.False(PayPalMailbox.IsGenuine([Header("Subject", "Du hast eine Zahlung erhalten")]));
        Assert.False(PayPalMailbox.IsGenuine(null));
    }

    private static PayPalMailbox.MessageHeader Header(string name, string value) => new() { Name = name, Value = value };
}
