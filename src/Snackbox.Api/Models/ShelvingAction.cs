using Snackbox.Api.Dtos;

namespace Snackbox.Api.Models;

public class ShelvingAction
{
    public int Id { get; set; }
    public int? ProductBatchId { get; set; }
    public int Quantity { get; set; }
    public ShelvingActionType Type { get; set; }
    public DateTime ActionAt { get; set; }
    public int? InvoiceItemId { get; set; }

    /// <summary>Set on the shelf removal a tablet pick booked: undoing the pick removes it again.</summary>
    public int? BarcodeScanId { get; set; }

    // Navigation properties
    public ProductBatch? ProductBatch { get; set; }
    public InvoiceItem? InvoiceItem { get; set; }
    public BarcodeScan? BarcodeScan { get; set; }
}
