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
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<Barcode> Barcodes { get; set; } = new List<Barcode>();
    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
    public ICollection<Withdrawal> Withdrawals { get; set; } = new List<Withdrawal>();
}
