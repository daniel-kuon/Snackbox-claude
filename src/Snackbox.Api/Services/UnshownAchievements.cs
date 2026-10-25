using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;

namespace Snackbox.Api.Services;

/// <summary>
/// Achievements a user earned but has not seen yet: handed out once and marked as shown. Users
/// with the tablet see them there (after Done); everyone else on the kiosk with their next scan.
/// </summary>
public static class UnshownAchievements
{
    public static async Task<List<AchievementDto>> ClaimAsync(ApplicationDbContext context, int userId)
    {
        var unshown = await context.UserAchievements
            .Include(ua => ua.Achievement)
            .Where(ua => ua.UserId == userId && !ua.HasBeenShown)
            .ToListAsync();

        foreach (var ua in unshown) ua.HasBeenShown = true;
        if (unshown.Count > 0) await context.SaveChangesAsync();

        return unshown.Select(ua => new AchievementDto
        {
            Id = ua.Achievement.Id,
            Code = ua.Achievement.Code,
            Name = ua.Achievement.Name,
            Description = ua.Achievement.Description,
            Category = ua.Achievement.Category.ToString(),
            ImageUrl = ua.Achievement.ImageUrl,
            EarnedAt = ua.EarnedAt
        }).ToList();
    }
}
