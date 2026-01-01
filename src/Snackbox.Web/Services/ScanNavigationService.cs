using Microsoft.AspNetCore.Components;
using Snackbox.Components.Services;

namespace Snackbox.Web.Services;

public class ScanNavigationService : IDisposable
{
    private readonly WindowsScannerListener _scannerListener;
    private readonly IScannerService _scannerService;
    private readonly AppStateService _appState;
    private readonly NavigationManager _navigation;

    public ScanNavigationService(
        WindowsScannerListener scannerListener,
        IScannerService scannerService,
        AppStateService appState,
        NavigationManager navigation)
    {
        _scannerListener = scannerListener;
        _scannerService = scannerService;
        _appState = appState;
        _navigation = navigation;

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
            // Try to scan the barcode through the scanner service
            var result = await _scannerService.ScanBarcodeAsync(code);

            if (result.IsSuccess)
            {
                // Check if user is admin
                if (result.IsAdmin)
                {
                    // Show navbar for admin
                    _appState.ShowNavbarForAdmin();
                }
                else
                {
                    // Hide navbar for regular users
                    _appState.HideNavbar();
                }

                // Navigate to scanner view (which handles the session)
                await _scannerService.ProcessBarcodeAsync(code);
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
