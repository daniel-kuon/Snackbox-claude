using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Snackbox.Api.Services;
using Xunit;

namespace Snackbox.Api.Tests.Services;

public class ProductImageSearchTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly List<string> _requested = new();

    public ProductImageSearchTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Products.Add(new Product
        {
            Id = 1, Name = "Snickers", CreatedAt = DateTime.UtcNow,
            Barcodes = { new ProductBarcode { Barcode = "5000159461122", CreatedAt = DateTime.UtcNow } }
        });
        _context.SaveChanges();
    }

    private ProductImageSearch Create(string? serpApiKey)
    {
        var lookup = new Mock<IBarcodeLookupService>();
        lookup.Setup(l => l.LookupBarcodeAsync("5000159461122")).ReturnsAsync(new BarcodeLookupResponseDto
        {
            Success = true,
            Product = new BarcodeLookupProductDto { Title = "Snickers 50 g", ImageUrl = "https://off/exact.jpg" }
        });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(new FakeHandler(_requested)));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ImageSearch:SerpApiKey"] = serpApiKey })
            .Build();

        return new ProductImageSearch(_context, lookup.Object, factory.Object, configuration, NullLogger<ProductImageSearch>.Instance);
    }

    [Fact]
    public async Task WithoutKey_OffersOpenFoodFactsOnly()
    {
        var result = await Create(null).SuggestAsync(1);

        Assert.False(result.GoogleImagesEnabled);
        Assert.Equal(new[] { "https://off/exact.jpg", "https://off/byname.jpg" }, result.Suggestions.Select(s => s.ImageUrl));
        Assert.DoesNotContain(_requested, u => u.Contains("serpapi"));
    }

    [Fact]
    public async Task WithKey_AddsGoogleImages()
    {
        var result = await Create("secret").SuggestAsync(1);

        Assert.True(result.GoogleImagesEnabled);
        var google = result.Suggestions.Where(s => s.Source == "Google Images").ToList();
        // The plain-http result is left out: the picture would be fetched without TLS
        Assert.Equal(new[] { "https://google/one.png" }, google.Select(s => s.ImageUrl));
        Assert.Equal("https://google/one-thumb.jpg", google[0].ThumbnailUrl);
    }

    [Theory]
    [InlineData("8.8.8.8", true)]
    [InlineData("2a00:1450:4001:80b::2004", true)]
    [InlineData("127.0.0.1", false)]
    [InlineData("10.1.2.3", false)]
    [InlineData("172.20.0.1", false)]
    [InlineData("192.168.178.20", false)]
    [InlineData("169.254.169.254", false)] // cloud metadata
    [InlineData("::1", false)]
    [InlineData("fd00::1", false)]
    [InlineData("::ffff:192.168.1.1", false)]
    [InlineData("64:ff9b::a00:1", false)]    // NAT64 of 10.0.0.1
    [InlineData("2002:c0a8:101::1", false)]  // 6to4 of 192.168.1.1
    [InlineData("2001:0:4136:e378::1", false)] // Teredo
    [InlineData("::a00:1", false)]           // IPv4-compatible
    [InlineData("2001:db8::1", false)]
    [InlineData("198.18.0.1", false)]
    [InlineData("192.0.0.8", false)]
    public void PublicInternet_RefusesLocalAddresses(string address, bool isPublic)
    {
        Assert.Equal(isPublic, PublicInternet.IsPublic(IPAddress.Parse(address)));
    }

    private sealed class FakeHandler(List<string> requested) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            lock (requested) requested.Add(url);

            var json = request.RequestUri.Host switch
            {
                "search.openfoodfacts.org" => """{"hits":[{"product_name":"Snickers","image_front_url":"https://off/byname.jpg"},{"product_name":"no picture"}]}""",
                "serpapi.com" => """{"images_results":[{"original":"https://google/one.png","thumbnail":"https://google/one-thumb.jpg","title":"Snickers"},{"original":"http://google/plain.png"}]}""",
                _ => "{}"
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") });
        }
    }

    public void Dispose() => _context.Dispose();
}
