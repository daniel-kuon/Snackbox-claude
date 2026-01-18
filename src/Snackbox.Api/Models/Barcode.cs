namespace Snackbox.Api.Models;

// Make base barcode abstract and split responsibilities into two concrete types.
public abstract class Barcode
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string Code { get; set; }
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<BarcodeScan> Scans { get; set; } = new List<BarcodeScan>();
}

public sealed class LoginBarcode : Barcode
{
}

public sealed class PurchaseBarcode : Barcode
{
}
