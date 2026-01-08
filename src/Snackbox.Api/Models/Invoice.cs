namespace Snackbox.Api.Models;

public class Invoice
{
    public int Id { get; set; }
    public required string InvoiceNumber { get; set; }
    public DateTime InvoiceDate { get; set; }
    public required string Supplier { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AdditionalCosts { get; set; }
    public decimal PriceReduction { get; set; }
    public int PaidByUserId { get; set; }
    public int? PaymentId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }

    // Navigation properties
    public User CreatedBy { get; set; } = null!;
    public User PaidBy { get; set; } = null!;
    public Payment? Payment { get; set; }
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
}
