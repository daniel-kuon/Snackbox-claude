namespace Snackbox.Api.Dtos;

public class ScanBarcodeResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    // User information
    public int UserId { get; set; }
    public required string Username { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsLoginOnly { get; set; } // True if this is a login-only barcode

    // Current purchase information
    public int PurchaseId { get; set; }
    public List<ScannedBarcodeDto> ScannedBarcodes { get; set; } = new();
    public decimal TotalAmount { get; set; }

    // User financial information
    public decimal Balance { get; set; } // Total spent - total paid (negative = owes money)
    public decimal LastPaymentAmount { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    
    // Recent purchases
    public List<RecentPurchaseDto> RecentPurchases { get; set; } = new();
    
    // Newly earned achievements (not yet shown to user)
    public List<AchievementDto> NewAchievements { get; set; } = new();
    
    // Applicable discounts
    public List<AppliedDiscountDto> ApplicableDiscounts { get; set; } = new();
    public decimal DiscountedAmount { get; set; } // Total amount after applying discounts
}

public class ScannedBarcodeDto
{
    public string BarcodeCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ScannedAt { get; set; }
}

public class RecentPurchaseDto
{
    public int PurchaseId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CompletedAt { get; set; }
    public int ItemCount { get; set; }
}
