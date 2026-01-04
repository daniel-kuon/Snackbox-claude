using Snackbox.Api.DTOs;
using Snackbox.Api.Mappers;
using Snackbox.Api.Models;
using Xunit;

namespace Snackbox.Api.Tests.Mappers;

public class ProductMapperTests
{
    [Fact]
    public void ToDto_ProductBarcode_MapsAllProperties()
    {
        // Arrange
        var productBarcode = new ProductBarcode
        {
            Id = 1,
            ProductId = 10,
            Barcode = "1234567890123",
            Quantity = 5,
            CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
        };

        // Act
        var dto = productBarcode.ToDto();

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal(10, dto.ProductId);
        Assert.Equal("1234567890123", dto.Barcode);
        Assert.Equal(5, dto.Quantity);
        Assert.Equal(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc), dto.CreatedAt);
    }

    [Fact]
    public void ToDtoList_ProductBarcodes_MapsAllItems()
    {
        // Arrange
        var barcodes = new List<ProductBarcode>
        {
            new ProductBarcode { Id = 1, ProductId = 10, Barcode = "111", Quantity = 1, CreatedAt = DateTime.UtcNow },
            new ProductBarcode { Id = 2, ProductId = 10, Barcode = "222", Quantity = 2, CreatedAt = DateTime.UtcNow }
        };

        // Act
        var dtos = barcodes.ToDtoList();

        // Assert
        Assert.Equal(2, dtos.Count);
        Assert.Equal("111", dtos[0].Barcode);
        Assert.Equal("222", dtos[1].Barcode);
    }

    [Fact]
    public void ToDto_Product_MapsAllPropertiesAndBarcodes()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            BestBeforeInStock = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            BestBeforeOnShelf = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            Barcodes = new List<ProductBarcode>
            {
                new ProductBarcode { Id = 1, ProductId = 1, Barcode = "123", Quantity = 1, CreatedAt = DateTime.UtcNow },
                new ProductBarcode { Id = 2, ProductId = 1, Barcode = "456", Quantity = 2, CreatedAt = DateTime.UtcNow }
            }
        };

        // Act
        var dto = product.ToDto();

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal("Test Product", dto.Name);
        Assert.Equal(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc), dto.CreatedAt);
        Assert.Equal(new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc), dto.BestBeforeInStock);
        Assert.Equal(new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc), dto.BestBeforeOnShelf);
        Assert.Equal(2, dto.Barcodes.Count);
        Assert.Equal("123", dto.Barcodes[0].Barcode);
        Assert.Equal("456", dto.Barcodes[1].Barcode);
    }

    [Fact]
    public void ToDto_Product_WithEmptyBarcodes_ReturnsEmptyList()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            CreatedAt = DateTime.UtcNow,
            Barcodes = new List<ProductBarcode>()
        };

        // Act
        var dto = product.ToDto();

        // Assert
        Assert.Empty(dto.Barcodes);
    }

    [Fact]
    public void ToDtoList_Products_MapsAllItems()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product { Id = 1, Name = "Product 1", CreatedAt = DateTime.UtcNow, Barcodes = new List<ProductBarcode>() },
            new Product { Id = 2, Name = "Product 2", CreatedAt = DateTime.UtcNow, Barcodes = new List<ProductBarcode>() }
        };

        // Act
        var dtos = products.ToDtoList();

        // Assert
        Assert.Equal(2, dtos.Count);
        Assert.Equal("Product 1", dtos[0].Name);
        Assert.Equal("Product 2", dtos[1].Name);
    }
}
