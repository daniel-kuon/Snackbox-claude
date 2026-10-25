using System.Globalization;

namespace Snackbox.Components.Services;

/// <summary>Where users pay - the kiosk's and the tablet's QR code.</summary>
public static class PayPal
{
    public const string MeUrl = "https://paypal.me/dkuon";

    // Invariant: a German "12,50" in the address is not an amount PayPal understands
    public static string PayUrl(decimal openAmount) =>
        $"{MeUrl}/{Math.Abs(openAmount).ToString("F2", CultureInfo.InvariantCulture)}";
}
