namespace Snackbox.Api.Dtos;

public class BackupMetadataDto
{
    public required string Id { get; set; }
    public required string FileName { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required string Type { get; set; }
    public required long FileSizeBytes { get; set; }
    public string? Md5Hash { get; set; }
    public string? CustomName { get; set; }
}
