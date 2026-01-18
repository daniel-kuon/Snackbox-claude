using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Snackbox.Api.Dtos;
using Snackbox.Api.Services;

namespace Snackbox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(ISettingsService settingsService, ILogger<SettingsController> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<ApplicationSettingsDto> GetSettings()
    {
        try
        {
            var settings = _settingsService.GetSettings();
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving settings");
            return StatusCode(500, "An error occurred while retrieving settings");
        }
    }

    [HttpPut]
    public async Task<ActionResult> UpdateSettings([FromBody] ApplicationSettingsDto settings)
    {
        try
        {
            var success = await _settingsService.UpdateSettingsAsync(settings);
            if (success)
            {
                return Ok(new { message = "Settings updated successfully. Restart the application for changes to take effect." });
            }

            return StatusCode(500, "Failed to update settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings");
            return StatusCode(500, "An error occurred while updating settings");
        }
    }

    [HttpPost("test/email")]
    public async Task<ActionResult<TestResultDto>> TestEmailSettings([FromBody] EmailSettingsDto settings)
    {
        try
        {
            var result = await _settingsService.TestEmailSettingsAsync(settings);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing email settings");
            return Ok(new TestResultDto { Success = false, Message = $"Test failed: {ex.Message}" });
        }
    }

    [HttpPost("test/barcode")]
    public async Task<ActionResult<TestResultDto>> TestBarcodeLookup([FromBody] BarcodeLookupSettingsDto settings)
    {
        try
        {
            var result = await _settingsService.TestBarcodeLookupAsync(settings);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing barcode lookup");
            return Ok(new TestResultDto { Success = false, Message = $"Test failed: {ex.Message}" });
        }
    }

    [HttpPost("test/backup")]
    public async Task<ActionResult<TestResultDto>> TestBackupSettings([FromBody] BackupSettingsDto settings)
    {
        try
        {
            var result = await _settingsService.TestBackupSettingsAsync(settings);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing backup settings");
            return Ok(new TestResultDto { Success = false, Message = $"Test failed: {ex.Message}" });
        }
    }
}
