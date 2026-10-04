namespace Snackbox.Api.Models;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public string? Email { get; set; } // Optional - user may not provide email
    public string? PasswordHash { get; set; } // Optional - if null, user can only login with barcode
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsRetired { get; set; } = false; // Special flag to mark users as retired
    public bool IsBetaTester { get; set; } = false; // Sees features whose audience is BetaTesters
    public DateTime CreatedAt { get; set; }

    /// <summary>T_User.UserID of the old Snackbox, set by the legacy import. Lets a re-run
    /// recognise users it already imported and lets the verification match the two sides.</summary>
    public int? LegacyUserId { get; set; }

    // Navigation properties
    public ICollection<Barcode> Barcodes { get; set; } = new List<Barcode>();
    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
    public ICollection<UserWizardStep> WizardSteps { get; set; } = new List<UserWizardStep>();
    public ICollection<Withdrawal> Withdrawals { get; set; } = new List<Withdrawal>();
}
