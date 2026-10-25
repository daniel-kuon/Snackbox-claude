using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Snackbox.Api.Services;
using Xunit;

namespace Snackbox.Api.Tests.Services;

public class ProductCatalogServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IBarcodeLookupService> _lookup = new();
    private readonly ProductCatalogService _service;

    public ProductCatalogServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new ProductCatalogService(_context, _lookup.Object, new Mock<IHttpClientFactory>().Object,
                                             new StockCalculationService(), NullLogger<ProductCatalogService>.Instance);

        var now = DateTime.UtcNow;
        _context.Products.AddRange(
            new Product { Id = 1, Name = "Mars", CreatedAt = now, Barcodes = { new ProductBarcode { Barcode = "111", Quantity = 1, CreatedAt = now } } },
            new Product { Id = 2, Name = "Ritter Sport", CreatedAt = now },
            new Product { Id = 3, Name = "Ritter Sport Marzipan", CreatedAt = now });
        _context.SaveChanges();
    }

    private static readonly (int, string)[] Products = [(1, "Mars"), (2, "Ritter Sport"), (3, "Ritter Sport Marzipan"), (4, "Chips Paprika"), (5, "Paprika Chips")];

    [Theory]
    [InlineData("Mars Riegel 51g", null, 1)]                 // whole name is in the title
    [InlineData("Marshmallows", null, null)]                 // a word, not a substring
    [InlineData("Marzipan 100g", "Ritter Sport", 3)]         // brand counts; the most specific name wins
    [InlineData("Ritter Sport Nuss", null, 2)]
    [InlineData("Chips Paprika 175g", null, null)]           // two names equally specific: ambiguous
    [InlineData("Haribo Goldbären", "Haribo", null)]
    [InlineData("Twix", "Mars Wrigley", null)]               // the brand alone is the manufacturer, not the product
    [InlineData("Bounty Trio 85g", "Mars norge as", null)]
    public void FindMatch_MatchesWholeWordsAndPrefersTheMostSpecificName(string title, string? brand, int? expected)
    {
        Assert.Equal(expected, ProductCatalogService.FindMatch(title, brand, Products));
    }

    [Fact]
    public async Task Resolve_KnownBarcode_DoesNotAskOnline()
    {
        var result = await _service.ResolveAsync(new BarcodeResolveRequest { Barcode = "111" });

        Assert.Equal(BarcodeResolveStatus.Known, result.Status);
        Assert.Equal(1, result.Product!.Id);
        _lookup.Verify(l => l.LookupBarcodeAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Resolve_UnknownBarcodeOfAKnownProduct_IsAddedToThatProduct()
    {
        SetupOnline("222", "Marzipan", "Ritter Sport");

        var result = await _service.ResolveAsync(new BarcodeResolveRequest { Barcode = "222" });

        Assert.Equal(BarcodeResolveStatus.AssignedToExisting, result.Status);
        Assert.Equal(3, result.Product!.Id);
        Assert.Equal(3, (await _context.ProductBarcodes.SingleAsync(b => b.Barcode == "222")).ProductId);
    }

    [Fact]
    public async Task Resolve_UnknownProduct_CreatesItFromTheOnlineData()
    {
        SetupOnline("333", "Goldbären 200g", "Haribo");

        var result = await _service.ResolveAsync(new BarcodeResolveRequest { Barcode = "333" });

        Assert.Equal(BarcodeResolveStatus.Created, result.Status);
        Assert.Equal("Haribo Goldbären 200g", result.Product!.Name);
        Assert.Equal(result.Product.Id, (await _context.ProductBarcodes.SingleAsync(b => b.Barcode == "333")).ProductId);
    }

    [Fact]
    public async Task Resolve_UnknownEverywhere_NeedsAName()
    {
        _lookup.Setup(l => l.LookupBarcodeAsync("444")).ReturnsAsync(new BarcodeLookupResponseDto { Success = false });

        var withoutName = await _service.ResolveAsync(new BarcodeResolveRequest { Barcode = "444" });
        Assert.Equal(BarcodeResolveStatus.NotFound, withoutName.Status);
        Assert.False(await _context.ProductBarcodes.AnyAsync(b => b.Barcode == "444"));

        var withName = await _service.ResolveAsync(new BarcodeResolveRequest { Barcode = "444", Name = "Club Mate" });
        Assert.Equal(BarcodeResolveStatus.Created, withName.Status);
        Assert.Equal("Club Mate", withName.Product!.Name);
    }

    [Fact]
    public async Task Resolve_ForceNewProduct_SkipsTheNameMatch()
    {
        SetupOnline("555", "Mars Riegel", null);

        var result = await _service.ResolveAsync(new BarcodeResolveRequest { Barcode = "555", ForceNewProduct = true });

        Assert.Equal(BarcodeResolveStatus.Created, result.Status);
        Assert.NotEqual(1, result.Product!.Id);
    }

    [Theory]
    [InlineData("image/jpeg", true)]
    [InlineData("image/png", true)]
    [InlineData("image/svg+xml", false)] // a document that can run script where it is shown
    [InlineData("text/html", false)]
    [InlineData(null, false)]
    public void OnlyRasterPicturesAreAccepted(string? contentType, bool allowed)
    {
        Assert.Equal(allowed, ProductCatalogService.IsAllowedImageType(contentType));
    }

    private void SetupOnline(string barcode, string title, string? brand) =>
        _lookup.Setup(l => l.LookupBarcodeAsync(barcode)).ReturnsAsync(new BarcodeLookupResponseDto
        {
            Success = true,
            Product = new BarcodeLookupProductDto { Title = title, Brand = brand, Barcode = barcode }
        });

    public void Dispose() => _context.Dispose();
}
