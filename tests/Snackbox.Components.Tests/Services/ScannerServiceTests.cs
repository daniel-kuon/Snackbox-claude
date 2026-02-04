using Microsoft.Extensions.Configuration;
using Moq;
using Snackbox.Api.Dtos;
using Snackbox.ApiClient;
using Snackbox.Components.Models;
using Snackbox.Components.Services;
using Xunit;

namespace Snackbox.Components.Tests.Services;

public class ScannerServiceTests
{
    private readonly Mock<IScannerApi> _scannerApiMock;
    private readonly Mock<IPurchasesApi> _purchasesApiMock;
    private readonly Mock<IPaymentsApi> _paymentsApiMock;
    private readonly IConfiguration _configuration;
    private readonly ScannerService _scannerService;

    public ScannerServiceTests()
    {
        // Setup mock Refit clients
        _scannerApiMock = new Mock<IScannerApi>();
        _purchasesApiMock = new Mock<IPurchasesApi>();
        _paymentsApiMock = new Mock<IPaymentsApi>();

        // Setup configuration
        var inMemorySettings = new Dictionary<string, string>
        {
            {"Scanner:TimeoutSeconds", "60"}
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        _scannerService = new ScannerService(_scannerApiMock.Object, _purchasesApiMock.Object, _paymentsApiMock.Object, _configuration);
    }

    [Fact]
    public async Task ProcessBarcodeAsync_FirstScan_StartsNewSession()
    {
        // Arrange
        var apiResponse = new ScanBarcodeResponse
        {
            Success = true,
            UserId = 1,
            Username = "testuser",
            ScannedBarcodes = new List<ScannedBarcodeDto>
            {
                new() { BarcodeCode = "TEST-5EUR", Amount = 5.00m, ScannedAt = DateTime.UtcNow }
            },
            Balance = -10.00m,
            LastPaymentAmount = 50.00m,
            LastPaymentDate = DateTime.UtcNow.AddDays(-5),
            RecentPurchases = new List<RecentPurchaseDto>(),
            NewAchievements = new List<AchievementDto>(),
            ApplicableDiscounts = new List<AppliedDiscountDto>()
        };

        _scannerApiMock.Setup(x => x.ScanBarcodeAsync(It.IsAny<ScanBarcodeRequest>()))
            .ReturnsAsync(apiResponse);

        PurchaseSession? capturedSession = null;
        _scannerService.OnPurchaseStarted += session => capturedSession = session;

        // Act
        await _scannerService.ProcessBarcodeAsync("TEST-5EUR");

        // Assert
        Assert.True(_scannerService.IsSessionActive);
        Assert.NotNull(_scannerService.CurrentSession);
        Assert.NotNull(capturedSession);
        Assert.Equal("testuser", capturedSession.UserName);
        Assert.Single(capturedSession.ScannedBarcodes);
        Assert.Equal(5.00m, capturedSession.ScannedBarcodes[0].Amount);
    }

    [Fact]
    public async Task ProcessBarcodeAsync_SecondScan_UpdatesExistingSession()
    {
        // Arrange - First scan
        var firstResponse = new ScanBarcodeResponse
        {
            Success = true,
            UserId = 1,
            Username = "testuser",
            ScannedBarcodes = new List<ScannedBarcodeDto>
            {
                new() { BarcodeCode = "TEST-5EUR", Amount = 5.00m, ScannedAt = DateTime.UtcNow }
            },
            Balance = -10.00m,
            LastPaymentAmount = 50.00m,
            LastPaymentDate = DateTime.UtcNow.AddDays(-5),
            RecentPurchases = new List<RecentPurchaseDto>(),
            NewAchievements = new List<AchievementDto>(),
            ApplicableDiscounts = new List<AppliedDiscountDto>()
        };

        _scannerApiMock.Setup(x => x.ScanBarcodeAsync(It.Is<ScanBarcodeRequest>(r => r.BarcodeCode == "TEST-5EUR")))
            .ReturnsAsync(firstResponse);
        await _scannerService.ProcessBarcodeAsync("TEST-5EUR");

        // Arrange - Second scan
        var secondResponse = new ScanBarcodeResponse
        {
            Success = true,
            UserId = 1,
            Username = "testuser",
            ScannedBarcodes = new List<ScannedBarcodeDto>
            {
                new() { BarcodeCode = "TEST-5EUR", Amount = 5.00m, ScannedAt = DateTime.UtcNow },
                new() { BarcodeCode = "TEST-10EUR", Amount = 10.00m, ScannedAt = DateTime.UtcNow }
            },
            Balance = -10.00m,
            LastPaymentAmount = 50.00m,
            LastPaymentDate = DateTime.UtcNow.AddDays(-5),
            RecentPurchases = new List<RecentPurchaseDto>(),
            NewAchievements = new List<AchievementDto>(),
            ApplicableDiscounts = new List<AppliedDiscountDto>()
        };

        _scannerApiMock.Setup(x => x.ScanBarcodeAsync(It.Is<ScanBarcodeRequest>(r => r.BarcodeCode == "TEST-10EUR")))
            .ReturnsAsync(secondResponse);

        PurchaseSession? capturedSession = null;
        _scannerService.OnPurchaseUpdated += session => capturedSession = session;

        // Act
        await _scannerService.ProcessBarcodeAsync("TEST-10EUR");

        // Assert
        Assert.NotNull(capturedSession);
        Assert.Equal(2, capturedSession.ScannedBarcodes.Count);
        Assert.Equal(15.00m, capturedSession.TotalAmount);
    }

    [Fact]
    public async Task ProcessBarcodeAsync_InvalidBarcode_DoesNotStartSession()
    {
        // Arrange
        var apiResponse = new ScanBarcodeResponse
        {
            Success = false,
            UserId = 0,
            Username = "",
            ScannedBarcodes = new List<ScannedBarcodeDto>(),
            Balance = 0m,
            LastPaymentAmount = 0m,
            LastPaymentDate = null,
            RecentPurchases = new List<RecentPurchaseDto>(),
            NewAchievements = new List<AchievementDto>(),
            ApplicableDiscounts = new List<AppliedDiscountDto>()
        };

        _scannerApiMock.Setup(x => x.ScanBarcodeAsync(It.IsAny<ScanBarcodeRequest>()))
            .ReturnsAsync(apiResponse);

        // Act
        await _scannerService.ProcessBarcodeAsync("INVALID");

        // Assert
        Assert.False(_scannerService.IsSessionActive);
        Assert.Null(_scannerService.CurrentSession);
    }

    [Fact]
    public async Task ProcessBarcodeAsync_EmptyBarcode_DoesNothing()
    {
        // Act
        await _scannerService.ProcessBarcodeAsync("");

        // Assert
        Assert.False(_scannerService.IsSessionActive);
    }

    [Fact]
    public async Task ProcessBarcodeAsync_HttpError_DoesNotStartSession()
    {
        // Arrange
        _scannerApiMock.Setup(x => x.ScanBarcodeAsync(It.IsAny<ScanBarcodeRequest>()))
            .ThrowsAsync(new HttpRequestException("Internal server error"));

        // Act
        await _scannerService.ProcessBarcodeAsync("TEST-5EUR");

        // Assert
        Assert.False(_scannerService.IsSessionActive);
    }

    [Fact]
    public async Task ResetSession_ClearsCurrentSession()
    {
        // Arrange
        var apiResponse = new ScanBarcodeResponse
        {
            Success = true,
            UserId = 1,
            Username = "testuser",
            ScannedBarcodes = new List<ScannedBarcodeDto>
            {
                new() { BarcodeCode = "TEST-5EUR", Amount = 5.00m, ScannedAt = DateTime.UtcNow }
            },
            Balance = -10.00m,
            LastPaymentAmount = 50.00m,
            LastPaymentDate = DateTime.UtcNow.AddDays(-5),
            RecentPurchases = new List<RecentPurchaseDto>(),
            NewAchievements = new List<AchievementDto>(),
            ApplicableDiscounts = new List<AppliedDiscountDto>()
        };

        _scannerApiMock.Setup(x => x.ScanBarcodeAsync(It.IsAny<ScanBarcodeRequest>()))
            .ReturnsAsync(apiResponse);
        await _scannerService.ProcessBarcodeAsync("TEST-5EUR");

        bool timeoutFired = false;
        _scannerService.OnPurchaseTimeout += () => timeoutFired = true;

        // Act
        _scannerService.ResetSession();

        // Assert
        Assert.False(_scannerService.IsSessionActive);
        Assert.Null(_scannerService.CurrentSession);
        Assert.True(timeoutFired);
    }

    [Fact]
    public async Task CompletePurchaseAsync_ClearsSessionAndFiresEvent()
    {
        // Arrange
        var apiResponse = new ScanBarcodeResponse
        {
            Success = true,
            UserId = 1,
            Username = "testuser",
            ScannedBarcodes = new List<ScannedBarcodeDto>
            {
                new() { BarcodeCode = "TEST-5EUR", Amount = 5.00m, ScannedAt = DateTime.UtcNow }
            },
            Balance = -10.00m,
            LastPaymentAmount = 50.00m,
            LastPaymentDate = DateTime.UtcNow.AddDays(-5),
            RecentPurchases = new List<RecentPurchaseDto>(),
            NewAchievements = new List<AchievementDto>(),
            ApplicableDiscounts = new List<AppliedDiscountDto>()
        };

        _scannerApiMock.Setup(x => x.ScanBarcodeAsync(It.IsAny<ScanBarcodeRequest>()))
            .ReturnsAsync(apiResponse);
        await _scannerService.ProcessBarcodeAsync("TEST-5EUR");

        bool completedFired = false;
        _scannerService.OnPurchaseCompleted += () => completedFired = true;

        // Act
        await _scannerService.CompletePurchaseAsync();

        // Assert
        Assert.False(_scannerService.IsSessionActive);
        Assert.True(completedFired);
    }

    [Fact]
    public void TimeoutSeconds_ReturnsConfiguredValue()
    {
        // Assert
        Assert.Equal(60, _scannerService.TimeoutSeconds);
    }

    [Fact]
    public async Task ProcessBarcodeAsync_DifferentUser_StartsNewSession()
    {
        // Arrange - First user scan
        var firstUserResponse = new ScanBarcodeResponse
        {
            Success = true,
            UserId = 1,
            Username = "user1",
            ScannedBarcodes = new List<ScannedBarcodeDto>
            {
                new() { BarcodeCode = "USER1-5EUR", Amount = 5.00m, ScannedAt = DateTime.UtcNow }
            },
            Balance = 0m,
            LastPaymentAmount = 0m,
            LastPaymentDate = null,
            RecentPurchases = new List<RecentPurchaseDto>(),
            NewAchievements = new List<AchievementDto>(),
            ApplicableDiscounts = new List<AppliedDiscountDto>()
        };

        _scannerApiMock.Setup(x => x.ScanBarcodeAsync(It.Is<ScanBarcodeRequest>(r => r.BarcodeCode == "USER1-5EUR")))
            .ReturnsAsync(firstUserResponse);
        await _scannerService.ProcessBarcodeAsync("USER1-5EUR");

        // Arrange - Second user scan
        var secondUserResponse = new ScanBarcodeResponse
        {
            Success = true,
            UserId = 2,
            Username = "user2",
            ScannedBarcodes = new List<ScannedBarcodeDto>
            {
                new() { BarcodeCode = "USER2-5EUR", Amount = 5.00m, ScannedAt = DateTime.UtcNow }
            },
            Balance = 0m,
            LastPaymentAmount = 0m,
            LastPaymentDate = null,
            RecentPurchases = new List<RecentPurchaseDto>(),
            NewAchievements = new List<AchievementDto>(),
            ApplicableDiscounts = new List<AppliedDiscountDto>()
        };

        _scannerApiMock.Setup(x => x.ScanBarcodeAsync(It.Is<ScanBarcodeRequest>(r => r.BarcodeCode == "USER2-5EUR")))
            .ReturnsAsync(secondUserResponse);

        PurchaseSession? newSession = null;
        _scannerService.OnPurchaseStarted += session => newSession = session;

        // Act
        await _scannerService.ProcessBarcodeAsync("USER2-5EUR");

        // Assert
        Assert.NotNull(newSession);
        Assert.Equal("user2", newSession.UserName);
        Assert.Equal("2", newSession.UserId);
    }

}
