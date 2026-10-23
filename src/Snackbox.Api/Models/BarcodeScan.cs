namespace Snackbox.Api.Models;

public class BarcodeScan
{
    public int Id { get; set; }
    public int PurchaseId { get; set; }
    public int BarcodeId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ScannedAt { get; set; }
    /// <summary>T_Posten.PostenID of the old Snackbox, set by the legacy import. The import
    /// skips rows it already has, so it can be re-run while both apps run in parallel.</summary>
    public Guid? LegacyPostenId { get; set; }

    /// <summary>Set when the item was picked on the tablet: the scan then books this product's
    /// price under the card code that opened the tablet session.</summary>
    public int? ProductId { get; set; }

    public string? TraceId { get; set; } // OpenTelemetry trace id of the request that recorded this scan (SigNoz deep link)

    // Navigation properties
    public Purchase Purchase { get; set; } = null!;
    public Barcode Barcode { get; set; } = null!;
    public Product? Product { get; set; }
}
