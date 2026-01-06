using Refit;
using Snackbox.Api.Dtos;

namespace Snackbox.ApiClient;

/// <summary>
/// Backup API endpoints
/// </summary>
public interface IBackupApi
{
    [Post("/api/backup/create")]
    Task<BackupMetadataDto> CreateBackupAsync();

    [Get("/api/backup/list")]
    Task<IEnumerable<BackupMetadataDto>> ListBackupsAsync();

    [Post("/api/backup/restore/{id}")]
    Task RestoreBackupAsync(string id);

    [Multipart]
    [Post("/api/backup/import")]
    Task<BackupMetadataDto> ImportBackupAsync([AliasAs("file")] StreamPart file);

    [Get("/api/backup/download/{id}")]
    Task<HttpResponseMessage> DownloadBackupAsync(string id);

    [Delete("/api/backup/{id}")]
    Task DeleteBackupAsync(string id);

    [Get("/api/backup/database/check")]
    Task<DatabaseCheckResponse> CheckDatabaseAsync();

    [Post("/api/backup/database/create-empty")]
    Task CreateEmptyDatabaseAsync();

    [Post("/api/backup/database/create-seeded")]
    Task CreateSeededDatabaseAsync();

    [Get("/api/backup/tools/check")]
    Task<ToolsCheckResponse> CheckPostgresToolsAsync();
}

public class DatabaseCheckResponse
{
    public bool Exists { get; set; }
}

public class ToolsCheckResponse
{
    public bool Available { get; set; }
    public string? Message { get; set; }
}
