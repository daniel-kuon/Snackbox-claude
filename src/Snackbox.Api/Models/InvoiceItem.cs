namespace Snackbox.Api.Models;

public class InvoiceItem
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int? ProductId { get; set; }
    public required string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime? BestBeforeDate { get; set; }
    public string? Notes { get; set; }
    public string? ArticleNumber { get; set; }

    // Navigation properties
    public Invoice Invoice { get; set; } = null!;
    public Product? Product { get; set; }
    public ICollection<ShelvingAction> ShelvingActions { get; set; } = new List<ShelvingAction>();
}
