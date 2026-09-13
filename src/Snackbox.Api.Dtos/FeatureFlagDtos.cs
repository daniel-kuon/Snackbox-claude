namespace Snackbox.Api.Dtos;

/// <summary>Who a feature is switched on for.</summary>
public enum FeatureAudience
{
    /// <summary>Nobody sees the feature.</summary>
    Disabled = 0,
    /// <summary>Only users marked as beta testers.</summary>
    BetaTesters = 1,
    /// <summary>All users.</summary>
    Everyone = 2
}

public class FeatureFlagDto
{
    public required string Key { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public FeatureAudience Audience { get; set; }
}

public class UpdateFeatureFlagDto
{
    public FeatureAudience Audience { get; set; }
}

public class MarkWizardStepsSeenDto
{
    public required string BarcodeCode { get; set; }
    public List<string> StepKeys { get; set; } = new();
}
