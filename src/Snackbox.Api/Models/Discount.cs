namespace Snackbox.Api.Models;

public class Discount
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public decimal MinimumPurchaseAmount { get; set; }
    public DiscountType Type { get; set; }
    public decimal Value { get; set; } // Either cents amount or percentage
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public enum DiscountType
{
    FixedAmount, // Reduces by a fixed cent amount
    Percentage   // Reduces by a percentage
}
