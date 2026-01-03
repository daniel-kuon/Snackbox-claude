using Refit;
using Snackbox.Api.Dtos;

namespace Snackbox.ApiClient;

/// <summary>
/// Scanner API endpoints
/// </summary>
public interface IScannerApi
{
    [Post("/api/scanner/scan")]
    Task<ScanBarcodeResponse> ScanBarcodeAsync([Body] ScanBarcodeRequest request);
}
