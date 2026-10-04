using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Snackbox.Api.Dtos;
using Snackbox.Api.Services;

namespace Snackbox.Api.Controllers;

/// <summary>
/// Checks GitHub for new Snackbox releases and installs them. Admin only - installing restarts
/// the whole machine's stack.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UpdatesController : ControllerBase
{
    private readonly IUpdateService _updates;
    private readonly ILogger<UpdatesController> _logger;

    public UpdatesController(IUpdateService updates, ILogger<UpdatesController> logger)
    {
        _updates = updates;
        _logger = logger;
    }

    /// <summary>Installed version plus the newest published release, if GitHub can be reached.</summary>
    [HttpGet("status")]
    public async Task<ActionResult<UpdateStatusDto>> Status(CancellationToken ct)
        => Ok(await _updates.GetStatusAsync(ct));

    /// <summary>
    /// Hands off to the updater and returns straight away - the update stops this very process,
    /// so the result shows up in the log rather than in a response.
    /// </summary>
    [HttpPost("install")]
    public async Task<ActionResult<InstallUpdateResponseDto>> Install([FromBody] InstallUpdateRequestDto request, CancellationToken ct)
    {
        try
        {
            return Ok(await _updates.InstallAsync(request.Tag, ct));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Update refused");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>The updater's log, so the admin page can show how the last update went.</summary>
    [HttpGet("log")]
    public ActionResult<UpdateLogDto> GetLog([FromQuery] int lines = 200)
        => Ok(_updates.GetLog(lines));
}
