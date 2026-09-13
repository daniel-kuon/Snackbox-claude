using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Snackbox.Api.Dtos;
using Snackbox.Api.Services;

namespace Snackbox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeatureFlagsController : ControllerBase
{
    private readonly IFeatureFlagService _featureFlags;
    private readonly ILogger<FeatureFlagsController> _logger;

    public FeatureFlagsController(IFeatureFlagService featureFlags, ILogger<FeatureFlagsController> logger)
    {
        _featureFlags = featureFlags;
        _logger = logger;
    }

    /// <summary>
    /// All flags with the audience they are switched on for. Anonymous: the kiosk admin
    /// screen has no logged-in user but still has to render the current state.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<FeatureFlagDto>>> GetAll()
    {
        var flags = await _featureFlags.GetAllAsync();
        return Ok(flags.Select(f => new FeatureFlagDto
        {
            Key = f.Key,
            Name = f.Name,
            Description = f.Description,
            Audience = f.Audience
        }).ToList());
    }

    /// <summary>Which features a specific user may see (resolves the beta-tester rule).</summary>
    [HttpGet("for-user/{userId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<List<string>>> GetForUser(int userId)
    {
        return Ok(await _featureFlags.GetEnabledForUserAsync(userId));
    }

    [HttpPut("{key}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<FeatureFlagDto>> Update(string key, [FromBody] UpdateFeatureFlagDto dto)
    {
        var flag = await _featureFlags.SetAudienceAsync(key, dto.Audience);
        if (flag == null)
            return NotFound(new { message = "Unknown feature flag" });

        _logger.LogInformation("Feature flag {Key} audience set to {Audience}", flag.Key, flag.Audience);

        return Ok(new FeatureFlagDto
        {
            Key = flag.Key,
            Name = flag.Name,
            Description = flag.Description,
            Audience = flag.Audience
        });
    }
}
