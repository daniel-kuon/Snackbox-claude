using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Snackbox.Api.Controllers;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Snackbox.Api.Services;
using Xunit;

namespace Snackbox.Api.Tests.Controllers;

public class ScannerControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ScannerController _controller;
    private readonly IConfiguration _configuration;

    public ScannerControllerTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        // Setup configuration
        var inMemorySettings = new Dictionary<string, string>
        {
            {"Scanner:TimeoutSeconds", "60"}
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        // Create achievement service for controller
        var achievementService = new AchievementService(_context);

        _controller = new ScannerController(_context, _configuration, achievementService);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            IsAdmin = false,
            CreatedAt = DateTime.UtcNow
        };

        var barcode1 = new PurchaseBarcode
        {
            Id = 1,
            UserId = 1,
            Code = "TEST-5EUR",
            Amount = 5.00m,
            IsActive = true,
            IsLoginOnly = false,
            CreatedAt = DateTime.UtcNow
        };

        var barcode2 = new PurchaseBarcode
        {
            Id = 2,
            UserId = 1,
            Code = "TEST-10EUR",
            Amount = 10.00m,
            IsActive = true,
            IsLoginOnly = false,
            CreatedAt = DateTime.UtcNow
        };

        var loginBarcode = new LoginBarcode
        {
            Id = 3,
            UserId = 1,
            Code = "TEST-LOGIN",
            Amount = 0m,
            IsActive = true,
            IsLoginOnly = true,
            CreatedAt = DateTime.UtcNow
        };

        var payment = new Payment
        {
            Id = 1,
            UserId = 1,
            Amount = 50.00m,
            PaidAt = DateTime.UtcNow.AddDays(-5),
            Notes = "Test payment"
        };

        _context.Users.Add(user);
        _context.Barcodes.AddRange(barcode1, barcode2, loginBarcode);
        _context.Payments.Add(payment);
        _context.SaveChanges();
    }

    [Fact]
    public async Task ScanBarcode_FirstScan_CreatesNewPurchaseWithScan()
    {
        // Arrange
        var request = new ScanBarcodeRequest { BarcodeCode = "TEST-5EUR" };

        // Act
        var result = await _controller.ScanBarcode(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ScanBarcodeResponse>(okResult.Value);

        Assert.True(response.Success);
        Assert.Equal(1, response.UserId);
        Assert.Equal("testuser", response.Username);
        Assert.Single(response.ScannedBarcodes);
        Assert.Equal(5.00m, response.TotalAmount);
        Assert.Equal("TEST-5EUR", response.ScannedBarcodes[0].BarcodeCode);
    }

    [Fact]
    public async Task ScanBarcode_SecondScanWithin60Seconds_AddsToExistingPurchase()
    {
        // Arrange
        var request1 = new ScanBarcodeRequest { BarcodeCode = "TEST-5EUR" };
        var request2 = new ScanBarcodeRequest { BarcodeCode = "TEST-10EUR" };

        // Act
        var result1 = await _controller.ScanBarcode(request1);
        var result2 = await _controller.ScanBarcode(request2);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result2.Result);
        var response = Assert.IsType<ScanBarcodeResponse>(okResult.Value);

        Assert.True(response.Success);
        Assert.Equal(2, response.ScannedBarcodes.Count);
        Assert.Equal(15.00m, response.TotalAmount);
        
        // Verify it's the same purchase
        var purchases = await _context.Purchases
            .Where(p => p.UserId == 1 && p.CompletedAt == null)
            .ToListAsync();
        Assert.Single(purchases);
    }

    [Fact]
    public async Task ScanBarcode_SecondScanAfter60Seconds_CreatesNewPurchase()
    {
        // Arrange
        var user = await _context.Users.FindAsync(1);
        var barcode = await _context.Barcodes.FirstAsync(b => b.Code == "TEST-5EUR");

        // Create an old purchase with a scan from 61 seconds ago
        var oldPurchase = new Purchase
        {
            UserId = user!.Id,
            CreatedAt = DateTime.UtcNow.AddSeconds(-65)
        };
        _context.Purchases.Add(oldPurchase);
        await _context.SaveChangesAsync();

        var oldScan = new BarcodeScan
        {
            PurchaseId = oldPurchase.Id,
            BarcodeId = barcode.Id,
            Amount = 5.00m,
            ScannedAt = DateTime.UtcNow.AddSeconds(-61)
        };
        _context.BarcodeScans.Add(oldScan);
        await _context.SaveChangesAsync();

        var request = new ScanBarcodeRequest { BarcodeCode = "TEST-10EUR" };

        // Act
        var result = await _controller.ScanBarcode(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ScanBarcodeResponse>(okResult.Value);

        Assert.True(response.Success);
        Assert.Single(response.ScannedBarcodes); // Only the new scan
        Assert.Equal(10.00m, response.TotalAmount);

        // Verify old purchase was completed
        var completedPurchase = await _context.Purchases.FindAsync(oldPurchase.Id);
        Assert.NotNull(completedPurchase!.CompletedAt);

        // Verify new purchase was created
        var activePurchases = await _context.Purchases
            .Where(p => p.UserId == 1 && p.CompletedAt == null)
            .ToListAsync();
        Assert.Single(activePurchases);
        Assert.NotEqual(oldPurchase.Id, activePurchases[0].Id);
    }

    [Fact]
    public async Task ScanBarcode_InvalidBarcodeCode_ReturnsError()
    {
        // Arrange
        var request = new ScanBarcodeRequest { BarcodeCode = "INVALID-CODE" };

        // Act
        var result = await _controller.ScanBarcode(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ScanBarcodeResponse>(okResult.Value);

        Assert.False(response.Success);
        Assert.Equal("Barcode not found", response.ErrorMessage);
    }

    [Fact]
    public async Task ScanBarcode_InactiveBarcode_AllowsScanWithInactiveFlag()
    {
        // Arrange
        var inactiveBarcode = new PurchaseBarcode
        {
            Id = 99,
            UserId = 1,
            Code = "INACTIVE-CODE",
            Amount = 5.00m,
            IsActive = false,
            IsLoginOnly = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Barcodes.Add(inactiveBarcode);
        await _context.SaveChangesAsync();

        var request = new ScanBarcodeRequest { BarcodeCode = "INACTIVE-CODE" };

        // Act
        var result = await _controller.ScanBarcode(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ScanBarcodeResponse>(okResult.Value);

        Assert.True(response.Success);
        Assert.True(response.IsUserInactive);
        Assert.Equal(1, response.UserId);
        Assert.Equal("testuser", response.Username);
        Assert.Single(response.ScannedBarcodes);
        Assert.Equal(5.00m, response.TotalAmount);
    }

    [Fact]
    public async Task ScanBarcode_CalculatesBalanceCorrectly()
    {
        // Arrange
        // User has paid 50.00m (seeded)
        // Create a completed purchase with 15.00m spent
        var user = await _context.Users.FindAsync(1);
        var barcode = await _context.Barcodes.FirstAsync(b => b.Code == "TEST-5EUR");

        var completedPurchase = new Purchase
        {
            UserId = user!.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            CompletedAt = DateTime.UtcNow.AddDays(-2).AddMinutes(5)
        };
        _context.Purchases.Add(completedPurchase);
        await _context.SaveChangesAsync();

        _context.BarcodeScans.AddRange(
            new BarcodeScan
            {
                PurchaseId = completedPurchase.Id,
                BarcodeId = barcode.Id,
                Amount = 5.00m,
                ScannedAt = DateTime.UtcNow.AddDays(-2)
            },
            new BarcodeScan
            {
                PurchaseId = completedPurchase.Id,
                BarcodeId = barcode.Id,
                Amount = 10.00m,
                ScannedAt = DateTime.UtcNow.AddDays(-2).AddMinutes(1)
            }
        );
        await _context.SaveChangesAsync();

        var request = new ScanBarcodeRequest { BarcodeCode = "TEST-5EUR" };

        // Act
        var result = await _controller.ScanBarcode(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ScanBarcodeResponse>(okResult.Value);

        // Balance = TotalSpent - TotalPaid = 15.00 - 50.00 = -35.00 (user has credit)
        Assert.Equal(-35.00m, response.Balance);
    }

    [Fact]
    public async Task ScanBarcode_ReturnsLastPaymentInformation()
    {
        // Arrange
        var request = new ScanBarcodeRequest { BarcodeCode = "TEST-5EUR" };

        // Act
        var result = await _controller.ScanBarcode(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ScanBarcodeResponse>(okResult.Value);

        Assert.Equal(50.00m, response.LastPaymentAmount);
        Assert.NotNull(response.LastPaymentDate);
    }

    [Fact]
    public async Task ScanBarcode_MultipleScans_MaintainsCorrectOrder()
    {
        // Arrange
        var requests = new[]
        {
            new ScanBarcodeRequest { BarcodeCode = "TEST-5EUR" },
            new ScanBarcodeRequest { BarcodeCode = "TEST-10EUR" },
            new ScanBarcodeRequest { BarcodeCode = "TEST-5EUR" }
        };

        // Act
        ScanBarcodeResponse? lastResponse = null;
        foreach (var request in requests)
        {
            var result = await _controller.ScanBarcode(request);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            lastResponse = Assert.IsType<ScanBarcodeResponse>(okResult.Value);
        }

        // Assert
        Assert.NotNull(lastResponse);
        Assert.Equal(3, lastResponse.ScannedBarcodes.Count);
        Assert.Equal(20.00m, lastResponse.TotalAmount);
        
        // Verify order is maintained
        Assert.True(lastResponse.ScannedBarcodes[0].ScannedAt <= lastResponse.ScannedBarcodes[1].ScannedAt);
        Assert.True(lastResponse.ScannedBarcodes[1].ScannedAt <= lastResponse.ScannedBarcodes[2].ScannedAt);
    }

    [Fact]
    public async Task ScanBarcode_EmptyOldPurchase_RemovesInsteadOfCompleting()
    {
        // Arrange
        var user = await _context.Users.FindAsync(1);

        // Create an empty purchase from 65 seconds ago
        var emptyPurchase = new Purchase
        {
            UserId = user!.Id,
            CreatedAt = DateTime.UtcNow.AddSeconds(-65)
        };
        _context.Purchases.Add(emptyPurchase);
        await _context.SaveChangesAsync();

        var request = new ScanBarcodeRequest { BarcodeCode = "TEST-5EUR" };

        // Act
        var result = await _controller.ScanBarcode(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ScanBarcodeResponse>(okResult.Value);

        Assert.True(response.Success);

        // Verify empty purchase was deleted
        var deletedPurchase = await _context.Purchases.FindAsync(emptyPurchase.Id);
        Assert.Null(deletedPurchase);
    }

    [Fact]
    public async Task ScanBarcode_LoginOnlyBarcode_ReturnsSuccessWithoutCreatingPurchase()
    {
        // Arrange
        var request = new ScanBarcodeRequest { BarcodeCode = "TEST-LOGIN" };
        var purchaseCountBefore = await _context.Purchases.CountAsync();

        // Act
        var result = await _controller.ScanBarcode(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ScanBarcodeResponse>(okResult.Value);

        Assert.True(response.Success);
        Assert.True(response.IsLoginOnly);
        Assert.Equal(1, response.UserId);
        Assert.Equal("testuser", response.Username);
        Assert.Empty(response.ScannedBarcodes);

        // Verify no purchase was created
        var purchaseCountAfter = await _context.Purchases.CountAsync();
        Assert.Equal(purchaseCountBefore, purchaseCountAfter);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
