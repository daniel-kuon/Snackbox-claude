namespace Snackbox.Api.Dtos;

public class DiscountDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public decimal MinimumPurchaseAmount { get; set; }
    public string Type { get; set; } = string.Empty; // "FixedAmount" or "Percentage"
    public decimal Value { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Set for a theme's discount (seeded): it only applies while that theme is on.</summary>
    public string? ThemeName { get; set; }
    public bool FirstPurchaseOnly { get; set; }
}

public class AppliedDiscountDto
{
    public int DiscountId { get; set; }
    public required string Name { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal DiscountAmount { get; set; } // Actual amount saved
}
