namespace Snackbox.Api.Models;

/// <summary>
/// Records that a user has seen one step of the introduction wizard. Tracking per
/// step (instead of a single "has seen intro" flag) means a step added later - for a
/// newly enabled feature - is shown to existing users without replaying the whole intro.
/// </summary>
public class UserWizardStep
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string StepKey { get; set; }
    public DateTime SeenAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}
