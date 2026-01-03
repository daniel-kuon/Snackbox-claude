using Snackbox.Api.DTOs;

namespace Snackbox.Api.Services;

public interface IBarcodeLookupService
{
    Task<BarcodeLookupResponseDto> LookupBarcodeAsync(string barcode);
}
