using Refit;
using Snackbox.Api.DTOs;

namespace Snackbox.ApiClient;

/// <summary>
/// Barcode Lookup API endpoints (for looking up product information from external services)
/// </summary>
public interface IBarcodeLookupApi
{
    [Get("/api/barcodelookup/{barcode}")]
    Task<BarcodeLookupResponseDto> LookupBarcodeAsync(string barcode);
}
