using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Snackbox.Api.Controllers;
using Snackbox.Api.Dtos;
using Snackbox.Api.External;
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

public class BarcodeLookupServiceTests
{
    private readonly Mock<ILogger<BarcodeLookupService>> _mockLogger;
    private readonly BarcodeLookupService _service;
    private readonly FakeExternalApi _fakeApi;

    public BarcodeLookupServiceTests()
    {
        _mockLogger = new Mock<ILogger<BarcodeLookupService>>();
        _fakeApi = new FakeExternalApi();
        _service = new BarcodeLookupService(_fakeApi, _mockLogger.Object);
    }

    [Fact]
    public async Task LookupBarcodeAsync_WithValidBarcode_ReturnsSuccess()
    {
        // Arrange
        var barcode = "1234567890123";
        _fakeApi.Response = new SearchUpcDataApiResponse
        {
            Upc = barcode,
            Name = "Test Product",
            Brand = "Test Brand",
            Description = "Test Description",
            Category = "Test Category"
        };

        // Act
        var result = await _service.LookupBarcodeAsync(barcode);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Product);
        Assert.Equal("Test Product", result.Product.Title);
        Assert.Equal("Test Brand", result.Product.Brand);
        Assert.Equal(barcode, result.Product.Barcode);
    }

    [Fact]
    public async Task LookupBarcodeAsync_WithNotFoundBarcode_ReturnsFailure()
    {
        // Arrange
        var barcode = "9999999999999";
        _fakeApi.Response = null; // Simulate not found

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
    public async Task LookupBarcodeAsync_WithGenericError_ReturnsFailure()
    {
        // Arrange
        var barcode = "1234567890123";
        _fakeApi.Throw = new Exception("Boom");

        // Act
        var result = await _service.LookupBarcodeAsync(barcode);

        // Assert
        Assert.False(result.Success);
        Assert.False(result.Success);
        Assert.Equal("An unexpected error occurred", result.ErrorMessage);
    }

    [Fact]
    public async Task LookupBarcodeAsync_WithNetworkError_ReturnsFailure()
    {
        // Arrange
        var barcode = "1234567890123";
        _fakeApi.Throw = new HttpRequestException("Network error");

        // Act
        var result = await _service.LookupBarcodeAsync(barcode);

        // Assert
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
