namespace Snackbox.Api.Models;

public class BarcodeScan
{
    public int Id { get; set; }
    public int PurchaseId { get; set; }
    public int BarcodeId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ScannedAt { get; set; }

    // Navigation properties
    public Purchase Purchase { get; set; } = null!;
    public Barcode Barcode { get; set; } = null!;
}
