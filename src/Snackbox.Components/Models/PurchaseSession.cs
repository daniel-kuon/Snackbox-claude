namespace Snackbox.Components.Models;

public class PurchaseSession
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public decimal OpenAmount { get; set; }
    public decimal LastPaymentAmount { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    public List<ScannedBarcode> ScannedBarcodes { get; set; } = new();
    public decimal TotalAmount => ScannedBarcodes.Sum(i => i.Amount);
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public List<RecentPurchase> RecentPurchases { get; set; } = new();
    public List<Achievement> NewAchievements { get; set; } = new();
    public bool IsUserInactive { get; set; }
}

public class ScannedBarcode
{
    public string BarcodeCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
}

public class RecentPurchase
{
    public int PurchaseId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CompletedAt { get; set; }
    public int ItemCount { get; set; }
}

public class Achievement
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime EarnedAt { get; set; }
}
