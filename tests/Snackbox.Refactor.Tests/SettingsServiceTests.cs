using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging;
using Snackbox.Api.Dtos;
using Snackbox.Api.External;
using Snackbox.Api.Services;

namespace Snackbox.Refactor.Tests;

public class SettingsServiceTests
{
    private readonly IConfiguration _cfg = new ConfigurationBuilder().Build();
    private readonly ILogger<SettingsService> _logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<SettingsService>();
    private readonly FakeEnv _env = new();

    [Fact]
    public async Task TestBarcodeLookupAsync_WithValidKey_ReturnsSuccess()
    {
        var factory = new FakeFactory(new FakeApi { Response = new SearchUpcDataApiResponse { Upc = "049000050103", Name = "Cola" } });
        var svc = new SettingsService(_cfg, _logger, _env, factory);

        var res = await svc.TestBarcodeLookupAsync(new BarcodeLookupSettingsDto { ApiKey = "abc" });

        Assert.True(res.Success);
        Assert.Contains("valid", res.Message, StringComparison.OrdinalIgnoreCase);
    }

    private class FakeFactory : IExternalBarcodeApiFactory
    {
        private readonly IExternalBarcodeApi _api;
        public FakeFactory(IExternalBarcodeApi api) => _api = api;
        public IExternalBarcodeApi Create(string apiKey) => _api;
    }

    private class FakeApi : IExternalBarcodeApi
    {
        public SearchUpcDataApiResponse? Response { get; set; }
        public Task<SearchUpcDataApiResponse?> GetProductAsync(string barcode) => Task.FromResult<SearchUpcDataApiResponse?>(Response);
    }

    private class FakeEnv : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Snackbox";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
