namespace Snackbox.Api.Models;

public class Barcode
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string Code { get; set; }
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
    public bool IsLoginOnly { get; set; } // If true, can only be used for authentication, not purchasing
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<BarcodeScan> Scans { get; set; } = new List<BarcodeScan>();
}
