using Snackbox.Api.Dtos;

namespace Snackbox.Api.Services;

public interface IBarcodeLookupService
{
    Task<BarcodeLookupResponseDto> LookupBarcodeAsync(string barcode);
}
