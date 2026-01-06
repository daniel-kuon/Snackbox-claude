namespace Snackbox.Api.Models;

public class BackupMetadata
{
    public required string Id { get; set; }
    public required string FileName { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required BackupType Type { get; set; }
    public required long FileSizeBytes { get; set; }
}

public enum BackupType
{
    Manual,
    Daily,
    Weekly,
    Monthly
}
