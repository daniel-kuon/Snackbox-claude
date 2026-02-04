using System.Net;
using Microsoft.Extensions.Logging;
using Snackbox.Api.Dtos;
using Snackbox.Api.External;
using Snackbox.Api.Services;

namespace Snackbox.Refactor.Tests;

public class BarcodeLookupServiceTests
{
    private readonly ILogger<BarcodeLookupService> _logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<BarcodeLookupService>();

    [Fact]
    public async Task Lookup_Success_MapsFields()
    {
        var fakeApi = new FakeExternalApi
        {
            Response = new SearchUpcDataApiResponse
            {
                Upc = "123",
                Name = "Cola",
                Brand = "Coke",
                Description = "Refreshing",
                Category = "Drinks"
            }
        };

        var svc = new BarcodeLookupService(fakeApi, _logger);

        var result = await svc.LookupBarcodeAsync("123");

        Assert.True(result.Success);
        Assert.NotNull(result.Product);
        Assert.Equal("Cola", result.Product!.Title);
        Assert.Equal("Coke", result.Product!.Brand);
        Assert.Equal("123", result.Product!.Barcode);
    }

    [Fact]
    public async Task Lookup_EmptyBarcode_Fails()
    {
        var svc = new BarcodeLookupService(new FakeExternalApi(), _logger);
        var result = await svc.LookupBarcodeAsync("");
        Assert.False(result.Success);
        Assert.Equal("Barcode cannot be empty", result.ErrorMessage);
    }

    [Fact]
    public async Task Lookup_NetworkError_Fails()
    {
        var fakeApi = new FakeExternalApi { Throw = new HttpRequestException("network") };
        var svc = new BarcodeLookupService(fakeApi, _logger);
        var result = await svc.LookupBarcodeAsync("123");
        Assert.False(result.Success);
        Assert.Equal("Network error occurred while looking up barcode", result.ErrorMessage);
    }

    private class FakeExternalApi : IExternalBarcodeApi
    {
        public SearchUpcDataApiResponse? Response { get; set; }
        public Exception? Throw { get; set; }

        public Task<SearchUpcDataApiResponse?> GetProductAsync(string barcode)
        {
            if (Throw != null) throw Throw;
            return Task.FromResult<SearchUpcDataApiResponse?>(Response);
        }
    }
}
