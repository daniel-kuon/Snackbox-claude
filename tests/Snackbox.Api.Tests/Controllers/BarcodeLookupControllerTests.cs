using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Snackbox.Api.Controllers;
using Snackbox.Api.Dtos;
using Snackbox.Api.Services;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Snackbox.Api.Tests.Controllers;

public class BarcodeLookupControllerTests
{
    private readonly Mock<IBarcodeLookupService> _mockService;
    private readonly Mock<ILogger<BarcodeLookupController>> _mockLogger;
    private readonly BarcodeLookupController _controller;

    public BarcodeLookupControllerTests()
    {
        _mockService = new Mock<IBarcodeLookupService>();
        _mockLogger = new Mock<ILogger<BarcodeLookupController>>();
        _controller = new BarcodeLookupController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task LookupBarcode_WithValidBarcode_ReturnsOkResult()
    {
        // Arrange
        var barcode = "1234567890123";
        var expectedResponse = new BarcodeLookupResponseDto
        {
            Success = true,
            Product = new BarcodeLookupProductDto
            {
                Title = "Test Product",
                Manufacturer = "Test Manufacturer",
                Brand = "Test Brand",
                Barcode = barcode
            }
        };

        _mockService.Setup(s => s.LookupBarcodeAsync(barcode))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.LookupBarcode(barcode);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<BarcodeLookupResponseDto>(okResult.Value);
        Assert.True(returnValue.Success);
        Assert.NotNull(returnValue.Product);
        Assert.Equal("Test Product", returnValue.Product.Title);
    }

    [Fact]
    public async Task LookupBarcode_WithNotFoundBarcode_ReturnsNotFound()
    {
        // Arrange
        var barcode = "9999999999999";
        var expectedResponse = new BarcodeLookupResponseDto
        {
            Success = false,
            ErrorMessage = "No product found for this barcode"
        };

        _mockService.Setup(s => s.LookupBarcodeAsync(barcode))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.LookupBarcode(barcode);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task LookupBarcode_WithEmptyBarcode_ReturnsBadRequest()
    {
        // Arrange
        var barcode = "";

        // Act
        var result = await _controller.LookupBarcode(barcode);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task LookupBarcode_WithWhitespaceBarcode_ReturnsBadRequest()
    {
        // Arrange
        var barcode = "   ";

        // Act
        var result = await _controller.LookupBarcode(barcode);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}

public class BarcodeLookupServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<BarcodeLookupService>> _mockLogger;
    private readonly BarcodeLookupService _service;

    public BarcodeLookupServiceTests()
    {
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<BarcodeLookupService>>();

        // Setup configuration to return a test API key
        _mockConfiguration.Setup(c => c["BarcodeLookup:ApiKey"])
            .Returns("test-api-key");

        _service = new BarcodeLookupService(_httpClient, _mockConfiguration.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    [Fact]
    public async Task LookupBarcodeAsync_WithValidBarcode_ReturnsSuccess()
    {
        // Arrange
        var barcode = "1234567890123";
        var apiResponse = new
        {
            products = new[]
            {
                new
                {
                    barcode_number = barcode,
                    title = "Test Product",
                    manufacturer = "Test Manufacturer",
                    brand = "Test Brand",
                    description = "Test Description",
                    category = "Test Category"
                }
            }
        };

        var responseContent = JsonSerializer.Serialize(apiResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.LookupBarcodeAsync(barcode);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Product);
        Assert.Equal("Test Product", result.Product.Title);
        Assert.Equal("Test Manufacturer", result.Product.Manufacturer);
        Assert.Equal("Test Brand", result.Product.Brand);
        Assert.Equal(barcode, result.Product.Barcode);
    }

    [Fact]
    public async Task LookupBarcodeAsync_WithNotFoundBarcode_ReturnsFailure()
    {
        // Arrange
        var barcode = "9999999999999";
        var apiResponse = new
        {
            products = Array.Empty<object>()
        };

        var responseContent = JsonSerializer.Serialize(apiResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.LookupBarcodeAsync(barcode);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("No product found for this barcode", result.ErrorMessage);
        Assert.Null(result.Product);
    }

    [Fact]
    public async Task LookupBarcodeAsync_WithEmptyBarcode_ReturnsFailure()
    {
        // Arrange
        var barcode = "";

        // Act
        var result = await _service.LookupBarcodeAsync(barcode);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Barcode cannot be empty", result.ErrorMessage);
    }

    [Fact]
    public async Task LookupBarcodeAsync_WithHttpError_ReturnsFailure()
    {
        // Arrange
        var barcode = "1234567890123";
        var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad Request")
        };

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.LookupBarcodeAsync(barcode);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid barcode format.", result.ErrorMessage);
    }

    [Fact]
    public async Task LookupBarcodeAsync_WithNetworkError_ReturnsFailure()
    {
        // Arrange
        var barcode = "1234567890123";

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _service.LookupBarcodeAsync(barcode);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Network error occurred while looking up barcode", result.ErrorMessage);
    }
}
