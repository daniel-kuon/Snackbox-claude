using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Snackbox.Components.Models;
using Snackbox.Components.Services;
using Xunit;

namespace Snackbox.Components.Tests.Services;

public class ScannerServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ScannerService _scannerService;

    public ScannerServiceTests()
    {
        // Setup mock HttpMessageHandler
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost")
        };

        // Setup configuration
        var inMemorySettings = new Dictionary<string, string>
        {
            {"Scanner:TimeoutSeconds", "60"}
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        _scannerService = new ScannerService(_httpClient, _configuration);
    }

    [Fact]
    public async Task ProcessBarcodeAsync_FirstScan_StartsNewSession()
    {
        // Arrange
        var apiResponse = new
        {
            Success = true,
            UserId = 1,
            Username = "testuser",
            PurchaseId = 1,
            ScannedBarcodes = new[]
            {
                new
                {
                    BarcodeCode = "TEST-5EUR",
                    Amount = 5.00m,
                    ScannedAt = DateTime.UtcNow
                }
            },
            TotalAmount = 5.00m,
            Balance = -10.00m,
            LastPaymentAmount = 50.00m,
            LastPaymentDate = DateTime.UtcNow.AddDays(-5)
        };

        SetupHttpResponse(HttpStatusCode.OK, apiResponse);

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
        var firstResponse = new
        {
            Success = true,
            UserId = 1,
            Username = "testuser",
            PurchaseId = 1,
            ScannedBarcodes = new[]
            {
                new { BarcodeCode = "TEST-5EUR", Amount = 5.00m, ScannedAt = DateTime.UtcNow }
            },
            TotalAmount = 5.00m,
            Balance = -10.00m,
            LastPaymentAmount = 50.00m,
            LastPaymentDate = (DateTime?)DateTime.UtcNow.AddDays(-5)
        };

        SetupHttpResponse(HttpStatusCode.OK, firstResponse);
        await _scannerService.ProcessBarcodeAsync("TEST-5EUR");

        // Arrange - Second scan
        var secondResponse = new
        {
            Success = true,
            UserId = 1,
            Username = "testuser",
            PurchaseId = 1,
            ScannedBarcodes = new[]
            {
                new { BarcodeCode = "TEST-5EUR", Amount = 5.00m, ScannedAt = DateTime.UtcNow },
                new { BarcodeCode = "TEST-10EUR", Amount = 10.00m, ScannedAt = DateTime.UtcNow }
            },
            TotalAmount = 15.00m,
            Balance = -10.00m,
            LastPaymentAmount = 50.00m,
            LastPaymentDate = (DateTime?)DateTime.UtcNow.AddDays(-5)
        };

        SetupHttpResponse(HttpStatusCode.OK, secondResponse);

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
        var apiResponse = new
        {
            Success = false,
            ErrorMessage = "Barcode not found",
            UserId = 0,
            Username = "",
            PurchaseId = 0,
            ScannedBarcodes = Array.Empty<object>(),
            TotalAmount = 0m,
            Balance = 0m,
            LastPaymentAmount = 0m,
            LastPaymentDate = (DateTime?)null
        };

        SetupHttpResponse(HttpStatusCode.OK, apiResponse);

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

        // Verify no HTTP call was made
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task ProcessBarcodeAsync_HttpError_DoesNotStartSession()
    {
        // Arrange
        SetupHttpResponse<object>(HttpStatusCode.InternalServerError, null);

        // Act
        await _scannerService.ProcessBarcodeAsync("TEST-5EUR");

        // Assert
        Assert.False(_scannerService.IsSessionActive);
    }

    [Fact]
    public async Task ResetSession_ClearsCurrentSession()
    {
        // Arrange
        var apiResponse = new
        {
            Success = true,
            UserId = 1,
            Username = "testuser",
            PurchaseId = 1,
            ScannedBarcodes = new[] { new { BarcodeCode = "TEST-5EUR", Amount = 5.00m, ScannedAt = DateTime.UtcNow } },
            TotalAmount = 5.00m,
            Balance = -10.00m,
            LastPaymentAmount = 50.00m,
            LastPaymentDate = (DateTime?)DateTime.UtcNow.AddDays(-5)
        };

        SetupHttpResponse(HttpStatusCode.OK, apiResponse);
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
        var apiResponse = new
        {
            Success = true,
            UserId = 1,
            Username = "testuser",
            PurchaseId = 1,
            ScannedBarcodes = new[] { new { BarcodeCode = "TEST-5EUR", Amount = 5.00m, ScannedAt = DateTime.UtcNow } },
            TotalAmount = 5.00m,
            Balance = -10.00m,
            LastPaymentAmount = 50.00m,
            LastPaymentDate = (DateTime?)DateTime.UtcNow.AddDays(-5)
        };

        SetupHttpResponse(HttpStatusCode.OK, apiResponse);
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
        var firstUserResponse = new
        {
            Success = true,
            UserId = 1,
            Username = "user1",
            PurchaseId = 1,
            ScannedBarcodes = new[] { new { BarcodeCode = "USER1-5EUR", Amount = 5.00m, ScannedAt = DateTime.UtcNow } },
            TotalAmount = 5.00m,
            Balance = 0m,
            LastPaymentAmount = 0m,
            LastPaymentDate = (DateTime?)null
        };

        SetupHttpResponse(HttpStatusCode.OK, firstUserResponse);
        await _scannerService.ProcessBarcodeAsync("USER1-5EUR");

        // Arrange - Second user scan
        var secondUserResponse = new
        {
            Success = true,
            UserId = 2,
            Username = "user2",
            PurchaseId = 2,
            ScannedBarcodes = new[] { new { BarcodeCode = "USER2-5EUR", Amount = 5.00m, ScannedAt = DateTime.UtcNow } },
            TotalAmount = 5.00m,
            Balance = 0m,
            LastPaymentAmount = 0m,
            LastPaymentDate = (DateTime?)null
        };

        SetupHttpResponse(HttpStatusCode.OK, secondUserResponse);

        PurchaseSession? newSession = null;
        _scannerService.OnPurchaseStarted += session => newSession = session;

        // Act
        await _scannerService.ProcessBarcodeAsync("USER2-5EUR");

        // Assert
        Assert.NotNull(newSession);
        Assert.Equal("user2", newSession.UserName);
        Assert.Equal("2", newSession.UserId);
    }

    private void SetupHttpResponse<T>(HttpStatusCode statusCode, T? content)
    {
        var response = new HttpResponseMessage(statusCode);

        if (content != null)
        {
            response.Content = JsonContent.Create(content);
        }

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(response);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
