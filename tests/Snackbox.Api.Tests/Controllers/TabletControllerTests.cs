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

    private readonly TabletSessionStore _sessions;

    public TabletControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Scanner:TimeoutSeconds"] = "60" })
            .Build();
        var sessions = _sessions = new TabletSessionStore(configuration);
        var achievements = new AchievementService(_context);

        _scanner = new ScannerController(_context, configuration, achievements, new FeatureFlagService(_context),
                                         NullLogger<ScannerController>.Instance, sessions, NoTabletDisplay);
        _tablet = new TabletController(_context, sessions, new StockCalculationService(), achievements,
                                       new Moq.Mock<IProductBestBeforeDateService>().Object, NoTabletDisplay);

        var now = DateTime.UtcNow;
        _context.Users.AddRange(
            new User { Id = 1, Username = "Tina", UsesTabletSelection = true, IsBetaTester = true, CreatedAt = now },
            new User { Id = 2, Username = "Carl", CreatedAt = now },
            new User { Id = 3, Username = "Nina", UsesTabletSelection = true, CreatedAt = now },
            new User { Id = 4, Username = "Ben", IsBetaTester = true, CreatedAt = now });
        _context.FeatureFlags.Add(new FeatureFlag { Id = 3, Key = FeatureFlagKeys.Tablet, Name = "Tablet", Description = "", Audience = FeatureAudience.BetaTesters, UpdatedAt = now });
        _context.Barcodes.AddRange(
            new PurchaseBarcode { Id = 1, UserId = 1, Code = "TINA-50", Amount = 0.50m, CreatedAt = now },
            new PurchaseBarcode { Id = 2, UserId = 2, Code = "CARL-50", Amount = 0.50m, CreatedAt = now },
            new PurchaseBarcode { Id = 3, UserId = 3, Code = "NINA-50", Amount = 0.50m, CreatedAt = now },
            new PurchaseBarcode { Id = 4, UserId = 4, Code = "BEN-30", Amount = 0.30m, CreatedAt = now });
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
    public async Task TabletUserWhoIsNoBetaTester_StillPaysTheCardAmount()
    {
        // The feature starts with beta testers only; ticking "picks on the tablet" is not enough
        var response = await Scan("NINA-50");

        Assert.False(response.IsTabletSession);
        Assert.Equal(0.50m, response.TotalAmount);
        Assert.False((await State()).Active);
    }

    [Fact]
    public async Task BetaTesterWithCard_SeesTheirScansOnTheTabletButCannotPick()
    {
        var response = await Scan("BEN-30");
        await Scan("BEN-30");

        Assert.False(response.IsTabletSession);
        var state = await State();
        Assert.True(state.Active);
        Assert.False(state.Picking);
        Assert.Equal(new[] { (0.30m, 2) }, state.CardScans.Select(c => (c.Amount, c.Count)));
        Assert.Equal(0.60m, state.OpenAmount);
        Assert.IsType<ConflictObjectResult>((await _tablet.AddItem(1)).Result);
    }

    [Fact]
    public async Task Introduction_IsShownUntilItWasSeen()
    {
        await Scan("TINA-50");
        Assert.True((await State()).ShowIntro);

        await _tablet.IntroSeen();
        await _tablet.IntroSeen();

        Assert.False((await State()).ShowIntro);
        Assert.Equal(1, await _context.UserWizardSteps.CountAsync(s => s.StepKey == TabletController.IntroStepKey));
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
    public async Task Done_ClosesThePurchase_AndNewPurchaseStartsANewOne()
    {
        await Scan("TINA-50");
        await _tablet.AddItem(1);
        var first = (await _context.BarcodeScans.SingleAsync()).PurchaseId;

        await _tablet.Complete();

        // The tablet stays on for a few seconds, the purchase is over
        var state = await State();
        Assert.True(state.Active);
        Assert.True(state.Completed);
        Assert.Empty(state.Items);

        // The products are hidden behind the summary: a pick needs New purchase first
        Assert.IsType<ConflictObjectResult>((await _tablet.AddItem(1)).Result);

        await _tablet.NewPurchase();
        await _tablet.AddItem(1);
        state = await State();
        Assert.False(state.Completed);
        Assert.Equal(0.80m, state.Total);
        Assert.Equal(2, await _context.Purchases.CountAsync());
        Assert.NotEqual(first, (await _context.BarcodeScans.OrderBy(s => s.Id).LastAsync()).PurchaseId);
    }

    [Fact]
    public async Task AfterDone_KeepingADialogOpen_DoesNotExtendTheWindowForANewPurchase()
    {
        await Scan("TINA-50");
        await _tablet.Complete();
        // Done was a while ago, a touch on a dialog kept the screen alive since
        _sessions.Start(_sessions.Current! with { CompletedAt = DateTime.UtcNow.AddSeconds(-30) });
        _tablet.Pause(true);

        Assert.IsType<ConflictObjectResult>((await _tablet.NewPurchase()).Result);
        Assert.IsType<ConflictObjectResult>((await _tablet.AddItem(1)).Result);
        Assert.Equal(0, await _context.BarcodeScans.CountAsync());
    }

    [Fact]
    public async Task Summary_ShowsTheClosedPurchase_TheMonthsPurchases_AndTheLastPayment()
    {
        _context.Payments.Add(new Payment { UserId = 1, Amount = 5m, PaidAt = DateTime.UtcNow.AddDays(-1) });
        _context.Payments.Add(new Payment { UserId = 2, Amount = 9m, PaidAt = DateTime.UtcNow }); // someone else's
        await _context.SaveChangesAsync();
        await Scan("TINA-50");
        await _tablet.AddItem(1);
        await _tablet.AddItem(1);
        await _tablet.Complete();

        var summary = (TabletSummaryDto)((OkObjectResult)(await _tablet.Summary()).Result!).Value!;

        Assert.Equal(2, summary.Items.Single().Quantity);
        Assert.Equal(1.60m, summary.Total);
        Assert.Equal(1.60m, summary.RecentPurchases.Single().Total);
        Assert.Equal(5m, summary.LastPayment!.Amount);
    }

    [Fact]
    public async Task AScanRightAfterDone_StartsANewPurchase()
    {
        await Scan("TINA-50");
        await _tablet.AddItem(1);
        await _tablet.Complete();

        await Scan("TINA-50");
        await _tablet.AddItem(1);

        Assert.Equal(2, await _context.Purchases.CountAsync());
    }

    [Fact]
    public async Task Done_WithoutAPick_LeavesNoEmptyPurchase()
    {
        await Scan("TINA-50");
        await _tablet.Complete();

        Assert.Equal(0, await _context.Purchases.CountAsync());
    }

    [Fact]
    public async Task Pause_StopsTheCountdown()
    {
        await Scan("TINA-50");
        _tablet.Pause(true);

        var state = await State();
        Assert.True(state.Paused);
        Assert.Equal(60, state.SecondsLeft);
    }

    [Fact]
    public async Task TabletUsers_GetTheirNewAchievementsOnTheTablet_OthersOnTheKiosk()
    {
        var now = DateTime.UtcNow;
        _context.Achievements.Add(new Achievement { Id = 1, Code = "TEST", Name = "Test", Description = "d", Category = AchievementCategory.SinglePurchase });
        _context.UserAchievements.AddRange(
            new UserAchievement { UserId = 1, AchievementId = 1, EarnedAt = now },
            new UserAchievement { UserId = 2, AchievementId = 1, EarnedAt = now });
        await _context.SaveChangesAsync();

        var tina = await Scan("TINA-50");
        Assert.Empty(tina.NewAchievements); // kept for the tablet
        var claimed = (List<AchievementDto>)((OkObjectResult)(await _tablet.ClaimAchievements()).Result!).Value!;
        Assert.Equal("Test", claimed.Single().Name);
        Assert.Empty((List<AchievementDto>)((OkObjectResult)(await _tablet.ClaimAchievements()).Result!).Value!);

        var carl = await Scan("CARL-50"); // no tablet feature: the kiosk shows it, as before
        Assert.Equal("Test", carl.NewAchievements.Single().Name);
    }

    private int OnShelf(int batchId) =>
        new StockCalculationService().CalculateShelfQuantity(_context.ShelvingActions.Where(a => a.ProductBatchId == batchId).ToList());

    [Fact]
    public async Task Picks_TakeItemsOffTheOldestBatchOnTheShelf_AndUndoPutsThemBack()
    {
        // A newer batch of Mars with stock, and the older one (batch 1) with 5 on the shelf
        var now = DateTime.UtcNow;
        _context.ProductBatches.Add(new ProductBatch { Id = 2, ProductId = 1, BestBeforeDate = now.AddMonths(6), CreatedAt = now });
        _context.ShelvingActions.Add(new ShelvingAction { ProductBatchId = 2, Quantity = 4, Type = ShelvingActionType.AddedToShelf, ActionAt = now });
        await _context.SaveChangesAsync();
        await Scan("TINA-50");

        await _tablet.AddItem(1);
        await _tablet.AddItem(1);
        Assert.Equal((3, 4), (OnShelf(1), OnShelf(2)));

        await _tablet.RemoveItem(1);
        Assert.Equal((4, 4), (OnShelf(1), OnShelf(2)));

        await _tablet.RemoveItem(1, all: true);
        Assert.Equal(5, OnShelf(1));
    }

    [Fact]
    public async Task AProductWithoutPrice_CannotBePicked()
    {
        await Scan("TINA-50");

        Assert.IsType<NotFoundObjectResult>((await _tablet.AddItem(3)).Result);
    }

    public void Dispose() => _context.Dispose();

    // Off in tests: no adb calls
    private static readonly TabletDisplay NoTabletDisplay = new(
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Tablet:WakeOnScan"] = "false" }).Build(),
        NullLogger<TabletDisplay>.Instance);
}
