namespace Snackbox.Api.Models;

/// <summary>
/// An admin-toggleable feature switch. Stored in the database so toggling takes
/// effect immediately, without restarting the API.
/// </summary>
public class FeatureFlag
{
    public int Id { get; set; }
    public required string Key { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Well-known feature flag keys.
/// </summary>
public static class FeatureFlagKeys
{
    /// <summary>Snackbox as an installable app on the user's phone (PWA + install guide).</summary>
    public const string MobileApp = "mobile_app";
}
