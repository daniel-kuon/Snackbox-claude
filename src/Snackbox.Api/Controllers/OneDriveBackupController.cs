using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Snackbox.Api.Dtos;
using Snackbox.Api.Services;

namespace Snackbox.Api.Controllers;

/// <summary>Connecting a OneDrive for encrypted off-site backups (Admin -> Backups).</summary>
[ApiController]
[Route("api/backup/onedrive")]
[Authorize(Roles = "Admin")]
public class OneDriveBackupController(OneDriveBackup oneDrive, IBackupService backups) : ControllerBase
{
    [HttpGet]
    public ActionResult<OneDriveStatusDto> GetStatus() => Ok(oneDrive.GetStatus());

    [HttpPut("settings")]
    public async Task<ActionResult<OneDriveStatusDto>> SaveSettings([FromBody] OneDriveSettingsDto settings)
    {
        try
        {
            await oneDrive.SaveSettingsAsync(settings);
            return Ok(oneDrive.GetStatus());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("connect")]
    public async Task<ActionResult<OneDriveSignInDto>> Connect()
    {
        try
        {
            return Ok(await oneDrive.StartSignInAsync());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("connect/poll")]
    public async Task<ActionResult<OneDrivePollDto>> Poll() => Ok(new OneDrivePollDto { Result = await oneDrive.PollSignInAsync() });

    [HttpPost("disconnect")]
    public async Task<ActionResult> Disconnect()
    {
        await oneDrive.DisconnectAsync();
        return NoContent();
    }

    /// <summary>Uploads the newest local backup now - to try the connection out.</summary>
    [HttpPost("upload-latest")]
    public async Task<ActionResult<OneDriveStatusDto>> UploadLatest()
    {
        var latest = (await backups.ListBackupsAsync()).OrderByDescending(b => b.CreatedAt).FirstOrDefault();
        if (latest == null) return BadRequest(new { message = "There is no backup yet - create one first." });

        var path = backups.GetBackupFilePath(latest);
        if (!System.IO.File.Exists(path)) return BadRequest(new { message = "The newest backup file is missing." });

        await oneDrive.UploadAsync(path);
        return Ok(oneDrive.GetStatus());
    }
}
