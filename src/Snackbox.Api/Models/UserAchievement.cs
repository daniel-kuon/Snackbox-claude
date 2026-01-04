namespace Snackbox.Api.Models;

public class UserAchievement
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int AchievementId { get; set; }
    public DateTime EarnedAt { get; set; }
    public bool HasBeenShown { get; set; } // Track if user has seen the achievement notification
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Achievement Achievement { get; set; } = null!;
}
