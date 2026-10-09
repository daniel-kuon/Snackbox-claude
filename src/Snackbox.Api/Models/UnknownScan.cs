namespace Snackbox.Api.Models;

/// <summary>
/// A kiosk scan of a code that is in no table - a new card nobody set up, a typo'd code, a
/// product barcode. Successful scans live in <see cref="BarcodeScan"/>; these exist so the
/// admin dashboard can show what people tried that did not work.
/// </summary>
public class UnknownScan
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public DateTime ScannedAt { get; set; }
}
