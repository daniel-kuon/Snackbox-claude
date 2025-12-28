namespace Snackbox.Api.Models;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<Barcode> Barcodes { get; set; } = new List<Barcode>();
    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
