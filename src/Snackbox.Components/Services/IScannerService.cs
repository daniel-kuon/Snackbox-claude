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

    Task<ScanResult> ScanBarcodeAsync(string barcode);
    Task ProcessBarcodeAsync(string barcode);
}

public class ScanResult
{
    public bool IsSuccess { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsLoginOnly { get; set; }
    public string? ErrorMessage { get; set; }
}
