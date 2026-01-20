namespace Snackbox.Api.Models;

public class PurchaseDiscount
{
    public int Id { get; set; }
    public int PurchaseId { get; set; }
    public int DiscountId { get; set; }
    public decimal DiscountAmount { get; set; } // The actual amount deducted

    // Navigation properties
    public Purchase Purchase { get; set; } = null!;
    public Discount Discount { get; set; } = null!;
}
