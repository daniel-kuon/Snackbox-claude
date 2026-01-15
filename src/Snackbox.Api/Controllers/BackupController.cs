using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<ActionResult<BackupMetadataDto>> CreateBackup()
    {
        try
        {
            // Proactively check for PostgreSQL tools to provide a clear error instead of a generic failure
            if (!await _backupService.ArePostgresToolsAvailableAsync())
            {
                return StatusCode(503, new { error = "PostgreSQL tools are not available", details = "Please install pg_dump and psql or run scripts/Install-PostgresTools.ps1 (Windows)." });
            }
            var backup = await _backupService.CreateBackupAsync(BackupType.Manual);
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
    /// Restores a backup by ID
    /// </summary>
    [HttpPost("restore/{id}")]
    public async Task<ActionResult> RestoreBackup(string id)
    {
        try
        {
            // Proactively check for PostgreSQL tools to provide a clear error instead of a generic failure
            if (!await _backupService.ArePostgresToolsAvailableAsync())
            {
                return StatusCode(503, new { error = "PostgreSQL tools are not available", details = "Please install pg_dump and psql or run scripts/Install-PostgresTools.ps1 (Windows)." });
            }
            await _backupService.RestoreBackupAsync(id);
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
    public async Task<ActionResult> CheckPostgresTools()
    {
        try
        {
            var available = await _backupService.ArePostgresToolsAvailableAsync();
            return Ok(new { available, message = available ? "PostgreSQL tools are available" : "PostgreSQL tools are not installed. Please run scripts/Install-PostgresTools.ps1" });
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
            FileSizeBytes = metadata.FileSizeBytes
        };
    }
}
