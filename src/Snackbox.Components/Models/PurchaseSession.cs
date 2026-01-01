namespace Snackbox.Components.Models;

public class PurchaseSession
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = "en";
    public decimal OpenAmount { get; set; }
    public decimal LastPaymentAmount { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    public List<ScannedBarcode> ScannedBarcodes { get; set; } = new();
    public decimal TotalAmount => ScannedBarcodes.Sum(i => i.Amount);
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
}

public class ScannedBarcode
{
    public string BarcodeCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
}
