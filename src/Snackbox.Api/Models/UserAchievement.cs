namespace Snackbox.Api.Models;

public class UserAchievement
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int AchievementId { get; set; }
    public DateTime EarnedAt { get; set; }
    public bool HasBeenShown { get; set; } // Track if user has seen the achievement notification
    public decimal? DebtAtEarning { get; set; } // For debt achievements, track debt level when earned

    // Navigation properties
    public User User { get; set; } = null!;
    public Achievement Achievement { get; set; } = null!;
}
