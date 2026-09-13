using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;

namespace Snackbox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeatureFlagsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FeatureFlagsController> _logger;

    public FeatureFlagsController(ApplicationDbContext context, ILogger<FeatureFlagsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// All flags with their state. Anonymous: the kiosk scan screen has no logged-in
    /// user but must still honour which features are switched on.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<FeatureFlagDto>>> GetAll()
    {
        var flags = await _context.FeatureFlags
            .OrderBy(f => f.Name)
            .Select(f => new FeatureFlagDto
            {
                Key = f.Key,
                Name = f.Name,
                Description = f.Description,
                IsEnabled = f.IsEnabled
            })
            .ToListAsync();

        return Ok(flags);
    }

    [HttpPut("{key}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<FeatureFlagDto>> Update(string key, [FromBody] UpdateFeatureFlagDto dto)
    {
        var flag = await _context.FeatureFlags.FirstOrDefaultAsync(f => f.Key == key);
        if (flag == null)
            return NotFound(new { message = "Unknown feature flag" });

        flag.IsEnabled = dto.IsEnabled;
        flag.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Feature flag {Key} set to {IsEnabled}", flag.Key, flag.IsEnabled);

        return Ok(new FeatureFlagDto
        {
            Key = flag.Key,
            Name = flag.Name,
            Description = flag.Description,
            IsEnabled = flag.IsEnabled
        });
    }
}
