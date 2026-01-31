namespace Snackbox.Api.Dtos;

public class UpdateCheckResultDto
{
    public string? CurrentVersion { get; set; }
    public string? LatestVersion { get; set; }
    public bool UpdateAvailable { get; set; }
    public string? LatestReleaseNotes { get; set; }
    public DateTimeOffset? LatestPublishedAt { get; set; }
}

public class UpdateApplyRequestDto
{
    public string? Version { get; set; }
    public bool AllowDowngrade { get; set; }
}

public class UpdateApplyResultDto
{
    public string Message { get; set; } = string.Empty;
    public string? TargetVersion { get; set; }
    public bool BackupCreated { get; set; }
    public string? BackupId { get; set; }
}
