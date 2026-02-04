using Refit;
using Snackbox.Api.Dtos;

namespace Snackbox.Api.External;

public interface IExternalBarcodeApi
{
    // GET https://searchupcdata.com/api/products/{barcode}
    [Get("/api/products/{barcode}")]
    Task<SearchUpcDataApiResponse?> GetProductAsync(string barcode);
}
