namespace Snackbox.Api.Models;

public class Achievement
{
    public int Id { get; set; }
    public required string Code { get; set; } // Unique identifier like "BIG_SPENDER_5"
    public required string Name { get; set; } // Display name
    public required string Description { get; set; }
    public AchievementCategory Category { get; set; }
    public string? ImageUrl { get; set; } // Optional custom image

    /// <summary>Seasonal achievements belong to a theme (ThemeKeys) and can only be earned while it is on.</summary>
    public string? ThemeKey { get; set; }

    // Navigation properties
    public ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
}

public enum AchievementCategory
{
    SinglePurchase,
    DailyActivity,
    Streak,
    Comeback,
    HighDebt,
    TotalSpent,
    TimeBased,
    Milestone,
    Special,
    Seasonal
}
