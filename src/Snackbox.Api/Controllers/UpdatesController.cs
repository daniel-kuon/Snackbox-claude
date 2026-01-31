using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Snackbox.Api.Dtos;
using Snackbox.Api.Services;

namespace Snackbox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UpdatesController : ControllerBase
{
    private readonly IUpdateService _updateService;
    private readonly ILogger<UpdatesController> _logger;

    public UpdatesController(IUpdateService updateService, ILogger<UpdatesController> logger)
    {
        _updateService = updateService;
        _logger = logger;
    }

    /// <summary>
    /// Checks for available updates
    /// </summary>
    [HttpGet("check")]
    public async Task<ActionResult<UpdateCheckResultDto>> Check(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _updateService.CheckForUpdatesAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for updates");
            return StatusCode(500, new { error = "Failed to check for updates", details = ex.Message });
        }
    }

    /// <summary>
    /// Starts an update to the requested version (or latest)
    /// </summary>
    [HttpPost("apply")]
    public async Task<ActionResult<UpdateApplyResultDto>> Apply([FromBody] UpdateApplyRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _updateService.ApplyUpdateAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, new { error = "Update blocked", details = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply update");
            return StatusCode(500, new { error = "Failed to apply update", details = ex.Message });
        }
    }
}
