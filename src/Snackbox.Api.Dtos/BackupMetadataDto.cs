namespace Snackbox.Api.Dtos;

public class BackupMetadataDto
{
    public required string Id { get; set; }
    public required string FileName { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required string Type { get; set; }
    public required long FileSizeBytes { get; set; }
}
