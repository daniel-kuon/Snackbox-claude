namespace Snackbox.Api.Models;

public class Achievement
{
    public int Id { get; set; }
    public required string Code { get; set; } // Unique identifier like "BIG_SPENDER_5"
    public required string Name { get; set; } // Display name
    public required string Description { get; set; }
    public AchievementCategory Category { get; set; }
    public string? ImageUrl { get; set; } // Optional custom image

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
    Special
}
