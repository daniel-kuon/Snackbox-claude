using Snackbox.Api.Dtos;

namespace Snackbox.Api.Models;

public class ShelvingAction
{
    public int Id { get; set; }
    public int ProductBatchId { get; set; }
    public int Quantity { get; set; }
    public ShelvingActionType Type { get; set; }
    public DateTime ActionAt { get; set; }
    public int? InvoiceItemId { get; set; }

    // Navigation properties
    public ProductBatch ProductBatch { get; set; } = null!;
    public InvoiceItem? InvoiceItem { get; set; }
}
