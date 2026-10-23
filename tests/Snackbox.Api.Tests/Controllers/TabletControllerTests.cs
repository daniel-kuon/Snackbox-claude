using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Snackbox.Api.Controllers;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Snackbox.Api.Services;
using Xunit;

namespace Snackbox.Api.Tests.Controllers;

public class TabletControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ScannerController _scanner;
    private readonly TabletController _tablet;

    public TabletControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Scanner:TimeoutSeconds"] = "60" })
            .Build();
        var sessions = new TabletSessionStore(configuration);
        var achievements = new AchievementService(_context);

        _scanner = new ScannerController(_context, configuration, achievements, new FeatureFlagService(_context),
                                         NullLogger<ScannerController>.Instance, sessions);
        _tablet = new TabletController(_context, sessions, new StockCalculationService(), achievements);

        var now = DateTime.UtcNow;
        _context.Users.AddRange(
            new User { Id = 1, Username = "Tina", UsesTabletSelection = true, CreatedAt = now },
            new User { Id = 2, Username = "Carl", CreatedAt = now });
        _context.Barcodes.AddRange(
            new PurchaseBarcode { Id = 1, UserId = 1, Code = "TINA-50", Amount = 0.50m, CreatedAt = now },
            new PurchaseBarcode { Id = 2, UserId = 2, Code = "CARL-50", Amount = 0.50m, CreatedAt = now });
        _context.Products.AddRange(
            new Product { Id = 1, Name = "Mars", Price = 0.80m, CreatedAt = now },
            new Product { Id = 2, Name = "Chips", Price = 1.20m, CreatedAt = now },
            new Product { Id = 3, Name = "No price", CreatedAt = now });
        _context.ProductBatches.AddRange(
            new ProductBatch { Id = 1, ProductId = 1, BestBeforeDate = now.AddMonths(3), CreatedAt = now },
            new ProductBatch { Id = 3, ProductId = 3, BestBeforeDate = now.AddMonths(3), CreatedAt = now });
        _context.ShelvingActions.AddRange(
            new ShelvingAction { ProductBatchId = 1, Quantity = 5, Type = ShelvingActionType.AddedToShelf, ActionAt = now },
            new ShelvingAction { ProductBatchId = 3, Quantity = 5, Type = ShelvingActionType.AddedToShelf, ActionAt = now });
        _context.SaveChanges();
    }

    private async Task<ScanBarcodeResponse> Scan(string code) =>
        (ScanBarcodeResponse)((OkObjectResult)(await _scanner.ScanBarcode(new ScanBarcodeRequest { BarcodeCode = code })).Result!).Value!;

    private async Task<TabletStateDto> State() => (TabletStateDto)((OkObjectResult)(await _tablet.GetState()).Result!).Value!;

    [Fact]
    public async Task Products_AreThoseWithAPriceOnTheShelf()
    {
        var products = (List<TabletProductDto>)((OkObjectResult)(await _tablet.GetProducts()).Result!).Value!;

        // Chips has no stock on the shelf, the third product no price
        Assert.Equal(new[] { "Mars" }, products.Select(p => p.Name));
    }

    [Fact]
    public async Task ConvertedUsersScan_OpensTheTabletAndBooksNothing()
    {
        var response = await Scan("TINA-50");

        Assert.True(response.IsTabletSession);
        Assert.Empty(response.ScannedBarcodes);
        Assert.Equal(0, await _context.BarcodeScans.CountAsync());

        var state = await State();
        Assert.True(state.Active);
        Assert.Equal("Tina", state.Username);
    }

    [Fact]
    public async Task Picks_AreBookedAtTheProductPriceAndCanBeChanged()
    {
        await Scan("TINA-50");

        await _tablet.AddItem(1);
        await _tablet.AddItem(1);
        await _tablet.AddItem(2);
        var state = await State();
        Assert.Equal(2.80m, state.Total);
        Assert.Equal(2, state.Items.Single(i => i.ProductId == 1).Quantity);

        await _tablet.RemoveItem(1);
        await _tablet.RemoveItem(2, all: true);
        state = await State();
        Assert.Equal(0.80m, state.Total);
        Assert.Equal(new[] { 1 }, state.Items.Select(i => i.ProductId));

        // One purchase, booked under the card's code, counted in the balance like any scan
        var scan = await _context.BarcodeScans.SingleAsync();
        Assert.Equal((1, 1, 0.80m), (scan.ProductId!.Value, scan.BarcodeId, scan.Amount));
        Assert.Equal(1, await _context.Purchases.CountAsync());
    }

    [Fact]
    public async Task ScanningAgain_ContinuesTheSamePurchase()
    {
        await Scan("TINA-50");
        await _tablet.AddItem(1);
        var response = await Scan("TINA-50");

        Assert.Equal(1, await _context.Purchases.CountAsync());
        Assert.Equal(0.80m, response.TotalAmount);
    }

    [Fact]
    public async Task AnotherUsersScan_EndsTheSession()
    {
        await Scan("TINA-50");
        var carl = await Scan("CARL-50");

        Assert.False(carl.IsTabletSession);
        Assert.False((await State()).Active);
        Assert.IsType<ConflictObjectResult>((await _tablet.AddItem(1)).Result);
    }

    [Fact]
    public async Task Done_EndsTheSessionButKeepsThePurchase()
    {
        await Scan("TINA-50");
        await _tablet.AddItem(1);

        _tablet.Complete();

        Assert.False((await State()).Active);
        Assert.Equal(1, await _context.BarcodeScans.CountAsync());
    }

    [Fact]
    public async Task AProductWithoutPrice_CannotBePicked()
    {
        await Scan("TINA-50");

        Assert.IsType<NotFoundObjectResult>((await _tablet.AddItem(3)).Result);
    }

    public void Dispose() => _context.Dispose();
}
