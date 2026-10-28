using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;

namespace Snackbox.Api.Controllers;

/// <summary>Seasonal looks of the kiosk. Which one shows is anonymous (the kiosk has no user); changing them is admin only.</summary>
[ApiController]
[Route("api/[controller]")]
public class ThemesController(ApplicationDbContext context, TimeProvider time) : ControllerBase
{
    /// <summary>The theme showing now: Always beats a date range, then the first by id.</summary>
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<ActionResult<ActiveThemeDto>> GetActive()
    {
        var now = time.GetLocalNow().DateTime;
        var themes = await context.Themes.OrderByDescending(t => t.Mode).ThenBy(t => t.Id).ToListAsync();
        return Ok(new ActiveThemeDto { Key = themes.FirstOrDefault(t => t.IsActiveOn(now))?.Key });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<ThemeDto>>> GetAll()
    {
        var now = time.GetLocalNow().DateTime;
        var themes = await context.Themes.OrderBy(t => t.Id).ToListAsync();
        return Ok(themes.Select(t => ToDto(t, now)).ToList());
    }

    [HttpPut("{key}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ThemeDto>> Update(string key, [FromBody] UpdateThemeDto dto)
    {
        var theme = await context.Themes.FirstOrDefaultAsync(t => t.Key == key);
        if (theme == null) return NotFound(new { message = "Unknown theme" });
        if (!ValidDay(dto.StartMonth, dto.StartDay) || !ValidDay(dto.EndMonth, dto.EndDay))
            return BadRequest(new { message = "Start and end must be real days of the year" });

        theme.Mode = dto.Mode;
        theme.StartMonth = dto.StartMonth;
        theme.StartDay = dto.StartDay;
        theme.EndMonth = dto.EndMonth;
        theme.EndDay = dto.EndDay;
        theme.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return Ok(ToDto(theme, time.GetLocalNow().DateTime));
    }

    // 29 February is allowed: in other years it counts as the 28th
    private static bool ValidDay(int month, int day) => month is >= 1 and <= 12 && day >= 1 && day <= DateTime.DaysInMonth(2024, month);

    private static ThemeDto ToDto(Theme t, DateTime now) => new()
    {
        Key = t.Key,
        Name = t.Name,
        Description = t.Description,
        Mode = t.Mode,
        StartMonth = t.StartMonth,
        StartDay = t.StartDay,
        EndMonth = t.EndMonth,
        EndDay = t.EndDay,
        Active = t.IsActiveOn(now)
    };
}
