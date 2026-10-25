namespace Snackbox.Api.Dtos;

/// <summary>
/// Well-known feature flag keys. Here rather than next to the model so the kiosk and the
/// admin pages use the same strings.
/// </summary>
public static class FeatureFlagKeys
{
    /// <summary>Snackbox as an installable app on the user's phone (PWA + install guide).</summary>
    public const string MobileApp = "mobile_app";

    /// <summary>
    /// Kiosk window in the background: minimized, never pulled to the front by a scan - the
    /// parallel run with the old Snackbox. A machine setting rather than a feature, so the
    /// Settings page shows it as its own switch (on = <see cref="FeatureAudience.Everyone"/>).
    /// </summary>
    public const string KioskBackground = "kiosk_background";

    /// <summary>
    /// Picking products on the tablet next to the scanner. Takes effect for users who also have
    /// "picks on the tablet" ticked; starts with beta testers only.
    /// </summary>
    public const string Tablet = "tablet";
}
