namespace Snackbox.Api.Models;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public string? PasswordHash { get; set; } // Optional - if null, user can only login with barcode
    public bool IsAdmin { get; set; }
    public string PreferredLanguage { get; set; } = "en"; // Default to English, supports "en" and "de"
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<Barcode> Barcodes { get; set; } = new List<Barcode>();
    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
