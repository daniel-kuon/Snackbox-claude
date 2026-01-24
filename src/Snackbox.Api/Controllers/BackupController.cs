using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Snackbox.Api.Attributes;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Snackbox.Api.Services;

namespace Snackbox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BackupController : ControllerBase
{
    private readonly IBackupService _backupService;
    private readonly ILogger<BackupController> _logger;

    public BackupController(IBackupService backupService, ILogger<BackupController> logger)
    {
        _backupService = backupService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a manual backup of the database
    /// </summary>
    [HttpPost("create")]
    public async Task<ActionResult<BackupMetadataDto>> CreateBackup([FromQuery] string? customName = null)
    {
        try
        {
            // Proactively check for PostgreSQL tools to provide a clear error instead of a generic failure
            if (!await _backupService.ArePostgresToolsAvailableAsync())
            {
                return StatusCode(503, new { error = "PostgreSQL tools are not available", details = "Please install PostgreSQL 17 by running: winget install -e --id PostgreSQL.PostgreSQL.17 (or use scripts/Install-PostgresTools.ps1)" });
            }
            var backup = await _backupService.CreateBackupAsync(BackupType.Manual, customName);

            // If backup is null, it means it was a duplicate
            if (backup == null)
            {
                return Ok(new { isDuplicate = true, message = "Backup is identical to an existing backup and was not created." });
            }

            return Ok(ToDto(backup));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup");
            return StatusCode(500, new { error = "Failed to create backup", details = ex.Message });
        }
    }

    /// <summary>
    /// Lists all available backups
    /// </summary>
    [HttpGet("list")]
    [AllowAnonymousIfNoDatabase]
    public async Task<ActionResult<List<BackupMetadataDto>>> ListBackups()
    {
        try
        {
            var backups = await _backupService.ListBackupsAsync();
            return Ok(backups.Select(ToDto).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list backups");
            return StatusCode(500, new { error = "Failed to list backups", details = ex.Message });
        }
    }

    /// <summary>
    /// Checks if database exists and would be overwritten by restore
    /// </summary>
    [HttpGet("restore/{id}/check")]
    public async Task<ActionResult> CheckRestoreImpact(string id)
    {
        try
        {
            var backups = await _backupService.ListBackupsAsync();
            var backup = backups.FirstOrDefault(b => b.Id == id);

            if (backup == null)
            {
                return NotFound(new { error = "Backup not found" });
            }

            var databaseExists = await _backupService.CheckDatabaseExistsAsync();

            return Ok(new
            {
                backupExists = true,
                databaseExists = databaseExists,
                requiresConfirmation = databaseExists,
                message = databaseExists
                    ? "Warning: This will replace your current database. All existing data will be lost unless you create a backup first."
                    : "A new database will be created from this backup."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check restore impact: {Id}", id);
            return StatusCode(500, new { error = "Failed to check restore impact", details = ex.Message });
        }
    }

    /// <summary>
    /// Restores a backup by ID
    /// </summary>
    [HttpPost("restore/{id}")]
    [AllowAnonymousIfNoDatabase]
    public async Task<ActionResult> RestoreBackup(string id, [FromQuery] bool createBackupBeforeRestore = false)
    {
        try
        {
            // Proactively check for PostgreSQL tools to provide a clear error instead of a generic failure
            if (!await _backupService.ArePostgresToolsAvailableAsync())
            {
                return StatusCode(503, new { error = "PostgreSQL tools are not available", details = "Please install PostgreSQL 17 by running: winget install -e --id PostgreSQL.PostgreSQL.17 (or use scripts/Install-PostgresTools.ps1)" });
            }
            await _backupService.RestoreBackupAsync(id, createBackupBeforeRestore);
            return Ok(new { message = "Backup restored successfully" });
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "Backup not found: {Id}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore backup: {Id}", id);
            return StatusCode(500, new { error = "Failed to restore backup", details = ex.Message });
        }
    }

    /// <summary>
    /// Imports a backup from an uploaded file
    /// </summary>
    [HttpPost("import")]
    [RequestSizeLimit(1_000_000_000)] // 1GB limit
    [AllowAnonymousIfNoDatabase]
    public async Task<ActionResult<BackupMetadataDto>> ImportBackup(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded" });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var backup = await _backupService.ImportBackupAsync(stream, file.FileName);
            return Ok(ToDto(backup));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import backup");
            return StatusCode(500, new { error = "Failed to import backup", details = ex.Message });
        }
    }

    /// <summary>
    /// Restores database directly from an uploaded backup file
    /// </summary>
    [HttpPost("restore-from-upload")]
    [RequestSizeLimit(1_000_000_000)] // 1GB limit
    [AllowAnonymousIfNoDatabase]
    public async Task<ActionResult> RestoreFromUpload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded" });
        }

        try
        {
            // Proactively check for PostgreSQL tools to provide a clear error instead of a generic failure
            if (!await _backupService.ArePostgresToolsAvailableAsync())
            {
                return StatusCode(503, new { error = "PostgreSQL tools are not available", details = "Please install PostgreSQL 17 by running: winget install -e --id PostgreSQL.PostgreSQL.17 (or use scripts/Install-PostgresTools.ps1)" });
            }

            using var stream = file.OpenReadStream();
            var backup = await _backupService.ImportBackupAsync(stream, file.FileName);
            await _backupService.RestoreBackupAsync(backup.Id, createBackupBeforeRestore: false);
            return Ok(new { message = "Backup restored successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore backup from upload");
            return StatusCode(500, new { error = "Failed to restore backup", details = ex.Message });
        }
    }

    /// <summary>
    /// Downloads a backup file
    /// </summary>
    [HttpGet("download/{id}")]
    public async Task<ActionResult> DownloadBackup(string id)
    {
        try
        {
            var backups = await _backupService.ListBackupsAsync();
            var backup = backups.FirstOrDefault(b => b.Id == id);

            if (backup == null)
            {
                return NotFound(new { error = "Backup not found" });
            }

            var stream = await _backupService.GetBackupStreamAsync(id);
            return File(stream, "application/octet-stream", backup.FileName);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "Backup file not found: {Id}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download backup: {Id}", id);
            return StatusCode(500, new { error = "Failed to download backup", details = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a backup by ID
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteBackup(string id)
    {
        try
        {
            await _backupService.DeleteBackupAsync(id);
            return Ok(new { message = "Backup deleted successfully" });
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "Backup not found: {Id}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete backup: {Id}", id);
            return StatusCode(500, new { error = "Failed to delete backup", details = ex.Message });
        }
    }

    /// <summary>
    /// Checks if the database exists and is accessible
    /// </summary>
    [HttpGet("database/check")]
    [AllowAnonymous]
    public async Task<ActionResult> CheckDatabase()
    {
        try
        {
            var exists = await _backupService.CheckDatabaseExistsAsync();
            return Ok(new { exists });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check database");
            return Ok(new { exists = false });
        }
    }

    /// <summary>
    /// Creates an empty database
    /// </summary>
    [HttpPost("database/create-empty")]
    [AllowAnonymousIfNoDatabase]
    public async Task<ActionResult> CreateEmptyDatabase()
    {
        try
        {
            await _backupService.CreateEmptyDatabaseAsync();
            return Ok(new { message = "Empty database created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create empty database");
            return StatusCode(500, new { error = "Failed to create empty database", details = ex.Message });
        }
    }

    /// <summary>
    /// Creates a database with seed data
    /// </summary>
    [HttpPost("database/create-seeded")]
    [AllowAnonymousIfNoDatabase]
    public async Task<ActionResult> CreateSeededDatabase()
    {
        try
        {
            await _backupService.CreateSeededDatabaseAsync();
            return Ok(new { message = "Seeded database created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create seeded database");
            return StatusCode(500, new { error = "Failed to create seeded database", details = ex.Message });
        }
    }

    /// <summary>
    /// Checks if PostgreSQL tools are available
    /// </summary>
    [HttpGet("tools/check")]
    [AllowAnonymousIfNoDatabase]
    public async Task<ActionResult> CheckPostgresTools()
    {
        try
        {
            var available = await _backupService.ArePostgresToolsAvailableAsync();
            return Ok(new { available, message = available ? "PostgreSQL tools are available" : "PostgreSQL tools are not installed. Run: winget install -e --id PostgreSQL.PostgreSQL.17" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check PostgreSQL tools");
            return Ok(new { available = false, message = "Failed to check PostgreSQL tools" });
        }
    }

    private static BackupMetadataDto ToDto(BackupMetadata metadata)
    {
        return new BackupMetadataDto
        {
            Id = metadata.Id,
            FileName = metadata.FileName,
            CreatedAt = metadata.CreatedAt,
            Type = metadata.Type.ToString(),
            FileSizeBytes = metadata.FileSizeBytes,
            Md5Hash = metadata.Md5Hash,
            CustomName = metadata.CustomName
        };
    }
}
