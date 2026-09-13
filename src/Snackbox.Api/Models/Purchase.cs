namespace Snackbox.Api.Models;

public class Purchase
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public PurchaseType Type { get; set; } = PurchaseType.Normal;
    public int? ReferencePurchaseId { get; set; } // For corrections
    public decimal? ManualAmount { get; set; } // For manual purchases and corrections

    /// <summary>
    /// Carried over from the previous Snackbox. Such a purchase counts towards money spent,
    /// streaks, milestones and comebacks, but not towards achievements that are about
    /// activity in this app (purchases per day, the snack birthday).
    /// </summary>
    public bool IsLegacyImport { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<BarcodeScan> Scans { get; set; } = new List<BarcodeScan>();
    public Purchase? ReferencePurchase { get; set; } // For corrections
    public ICollection<PurchaseDiscount> AppliedDiscounts { get; set; } = new List<PurchaseDiscount>();
}
