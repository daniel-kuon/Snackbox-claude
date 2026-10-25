using Snackbox.Api.Dtos;
using Snackbox.Components.Models;

namespace Snackbox.Components.Services;

public interface IScannerService
{
    event Action<PurchaseSession>? OnPurchaseStarted;
    event Action<PurchaseSession>? OnPurchaseUpdated;
    event Action? OnPurchaseCompleted;
    event Action? OnPurchaseTimeout;

    PurchaseSession? CurrentSession { get; }
    bool IsSessionActive { get; }
    int TimeoutSeconds { get; }

    Task<ScanResult> ProcessBarcodeAsync(string barcode);
    void ResetSession();
    void SignalActivity();
    void PauseTimeout();
    void ResumeTimeout();

    /// <summary>
    /// Takes over what the user picked on the tablet so far (items, totals, open amount) and
    /// restarts the timeout. A tablet session that ended (Done, someone else scanned, timeout)
    /// ends the kiosk's session too.
    /// </summary>
    void ApplyTabletState(TabletStateDto state);
    Task<IEnumerable<PurchaseDto>> GetMyPurchasesAsync();
    Task<IEnumerable<PaymentDto>> GetMyPaymentsAsync();
}

public class ScanResult
{
    public bool IsSuccess { get; set; }
    public bool IsLoginOnly { get; set; }
    public string? ErrorMessage { get; set; }
}
