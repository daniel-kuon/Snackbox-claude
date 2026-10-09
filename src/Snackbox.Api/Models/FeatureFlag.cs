using Snackbox.Api.Dtos;

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

    /// <summary>Who the feature is on for: nobody, beta testers only, or everyone.</summary>
    public FeatureAudience Audience { get; set; } = FeatureAudience.Disabled;

    public DateTime UpdatedAt { get; set; }
}
