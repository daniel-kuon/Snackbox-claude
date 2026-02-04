using System.Net.Http.Headers;
using Refit;

namespace Snackbox.Api.External;

public interface IExternalBarcodeApiFactory
{
    IExternalBarcodeApi Create(string apiKey);
}

public class ExternalBarcodeApiFactory : IExternalBarcodeApiFactory
{
    private static readonly Uri BaseAddress = new("https://searchupcdata.com");

    public IExternalBarcodeApi Create(string apiKey)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = BaseAddress
        };
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        return RestService.For<IExternalBarcodeApi>(httpClient);
    }
}
