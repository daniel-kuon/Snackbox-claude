namespace Snackbox.Api.Dtos;

public class UpdateStatusDto
{
    /// <summary>The release tag this installation has checked out, or a short commit id.</summary>
    public string CurrentVersion { get; set; } = "";

    /// <summary>False when this is not a git checkout - a developer build, for instance.</summary>
    public bool CanUpdate { get; set; }

    /// <summary>Why updating is not possible, when it is not.</summary>
    public string? Blocker { get; set; }

    public ReleaseDto? Latest { get; set; }
    public bool UpdateAvailable { get; set; }

    /// <summary>Set when GitHub could not be reached; the current version is still reported.</summary>
    public string? CheckError { get; set; }
}

public class ReleaseDto
{
    public string Tag { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Notes { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? Url { get; set; }
    public bool PreRelease { get; set; }
}

public class InstallUpdateRequestDto
{
    /// <summary>Release tag to move to. Empty means the newest release.</summary>
    public string? Tag { get; set; }
}

public class InstallUpdateResponseDto
{
    public bool Started { get; set; }
    public string Tag { get; set; } = "";
    public string Message { get; set; } = "";
}

public class UpdateLogDto
{
    public string[] Lines { get; set; } = [];
    public DateTime? LastWrite { get; set; }
}
