namespace Snackbox.Api.Dtos;

public class OneDriveStatusDto
{
    public string? ClientId { get; set; }
    public bool Connected { get; set; }
    public string? Account { get; set; }
    public bool HasPassphrase { get; set; }
    public DateTime? LastUploadAt { get; set; }
    public string? LastUploadFile { get; set; }
    public string? LastError { get; set; }
    public DateTime? LastErrorAt { get; set; }
    public int Keep { get; set; }
}

public class OneDriveSettingsDto
{
    public string? ClientId { get; set; }

    /// <summary>Only sent to change it; never returned.</summary>
    public string? Passphrase { get; set; }
}

public class OneDriveSignInDto
{
    public required string UserCode { get; set; }
    public required string VerificationUri { get; set; }
    public int ExpiresInSeconds { get; set; }
    public int IntervalSeconds { get; set; }
}

public class OneDrivePollDto
{
    /// <summary>"pending", "connected", "expired" or "failed: ..."</summary>
    public required string Result { get; set; }
}
