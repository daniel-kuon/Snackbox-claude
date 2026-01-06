using Snackbox.Api.Models;

namespace Snackbox.Api.Services;

public interface IBackupService
{
    /// <summary>
    /// Creates a backup of the entire database
    /// </summary>
    /// <param name="type">Type of backup (Manual, Daily, Weekly, Monthly)</param>
    /// <returns>Metadata of the created backup</returns>
    Task<BackupMetadata> CreateBackupAsync(BackupType type);

    /// <summary>
    /// Lists all available backups on disk
    /// </summary>
    /// <returns>List of backup metadata</returns>
    Task<List<BackupMetadata>> ListBackupsAsync();

    /// <summary>
    /// Restores a backup by its ID
    /// </summary>
    /// <param name="backupId">ID of the backup to restore</param>
    Task RestoreBackupAsync(string backupId);

    /// <summary>
    /// Imports a backup from a file stream
    /// </summary>
    /// <param name="fileStream">Stream containing the backup file</param>
    /// <param name="fileName">Original filename</param>
    /// <returns>Metadata of the imported backup</returns>
    Task<BackupMetadata> ImportBackupAsync(Stream fileStream, string fileName);

    /// <summary>
    /// Deletes a backup by its ID
    /// </summary>
    /// <param name="backupId">ID of the backup to delete</param>
    Task DeleteBackupAsync(string backupId);

    /// <summary>
    /// Cleans up old backups based on retention policy
    /// Daily backups: kept for 1 month
    /// Weekly backups: kept for 3 months
    /// Monthly backups: kept forever
    /// </summary>
    Task CleanupOldBackupsAsync();

    /// <summary>
    /// Gets a backup file stream for download/email
    /// </summary>
    /// <param name="backupId">ID of the backup</param>
    /// <returns>File stream of the backup</returns>
    Task<Stream> GetBackupStreamAsync(string backupId);

    /// <summary>
    /// Checks if the database exists and is accessible
    /// </summary>
    Task<bool> CheckDatabaseExistsAsync();

    /// <summary>
    /// Creates an empty database with all migrations applied
    /// </summary>
    Task CreateEmptyDatabaseAsync();

    /// <summary>
    /// Creates a database with seed data
    /// </summary>
    Task CreateSeededDatabaseAsync();

    /// <summary>
    /// Checks if PostgreSQL tools (pg_dump, psql) are available
    /// </summary>
    /// <returns>True if tools are available, false otherwise</returns>
    Task<bool> ArePostgresToolsAvailableAsync();
}
