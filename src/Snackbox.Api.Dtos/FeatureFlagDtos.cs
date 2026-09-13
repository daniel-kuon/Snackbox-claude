namespace Snackbox.Api.Dtos;

public class FeatureFlagDto
{
    public required string Key { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public bool IsEnabled { get; set; }
}

public class UpdateFeatureFlagDto
{
    public bool IsEnabled { get; set; }
}

public class MarkWizardStepsSeenDto
{
    public required string BarcodeCode { get; set; }
    public List<string> StepKeys { get; set; } = new();
}
