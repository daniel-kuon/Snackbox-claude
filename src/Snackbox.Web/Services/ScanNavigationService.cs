using Snackbox.Components.Services;

namespace Snackbox.Web.Services;

public partial class ScanNavigationService : IDisposable
{
    private readonly WindowsScannerListener _scannerListener;
    private readonly IScannerService _scannerService;

    public ScanNavigationService(
        WindowsScannerListener scannerListener,
        IScannerService scannerService)
    {
        _scannerListener = scannerListener;
        _scannerService = scannerService;

        _scannerListener.CodeReceived += HandleCodeScanned;
    }

    public void Start()
    {
        _scannerListener.Start();
    }

    private async void HandleCodeScanned(string code)
    {
        try
        {
            // Process the barcode through the scanner service
            var result = await _scannerService.ProcessBarcodeAsync(code);

            if (!result.IsSuccess)
            {
                Console.WriteLine($"Error processing scanned code: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            // Log error or show notification
            Console.WriteLine($"Error processing scanned code: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _scannerListener.CodeReceived -= HandleCodeScanned;
        _scannerListener.Stop();
    }
}
