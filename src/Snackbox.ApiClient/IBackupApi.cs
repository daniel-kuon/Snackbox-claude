using Refit;
using Snackbox.Api.Dtos;

namespace Snackbox.ApiClient;

/// <summary>
/// Backup API endpoints
/// </summary>
public interface IBackupApi
{
    [Post("/api/backup/create")]
    Task<object> CreateBackupAsync([Query] string? customName = null);

    [Get("/api/backup/list")]
    Task<IEnumerable<BackupMetadataDto>> ListBackupsAsync();

    [Get("/api/backup/restore/{id}/check")]
    Task<RestoreImpactResponse> CheckRestoreImpactAsync(string id);

    [Post("/api/backup/restore/{id}")]
    Task RestoreBackupAsync(string id, [Query] bool createBackupBeforeRestore = false);

    [Multipart]
    [Post("/api/backup/import")]
    Task<BackupMetadataDto> ImportBackupAsync([AliasAs("file")] StreamPart file);

    [Multipart]
    [Post("/api/backup/restore-from-upload")]
    Task RestoreFromUploadAsync([AliasAs("file")] StreamPart file);

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

public class RestoreImpactResponse
{
    public bool BackupExists { get; set; }
    public bool DatabaseExists { get; set; }
    public bool RequiresConfirmation { get; set; }
    public string? Message { get; set; }
}
