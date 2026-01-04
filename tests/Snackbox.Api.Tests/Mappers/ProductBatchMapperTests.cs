using Snackbox.Api.Dtos;
using Snackbox.Api.Mappers;
using Snackbox.Api.Models;
using Xunit;

namespace Snackbox.Api.Tests.Mappers;

public class ProductBatchMapperTests
{
    [Fact]
    public void ToDto_ProductBatch_MapsAllBasicProperties()
    {
        // Arrange
        var batch = new ProductBatch
        {
            Id = 1,
            ProductId = 10,
            BestBeforeDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
        };

        // Act
        var dto = batch.ToDto();

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal(10, dto.ProductId);
        Assert.Equal(new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc), dto.BestBeforeDate);
        Assert.Equal(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc), dto.CreatedAt);
        Assert.Null(dto.ProductName); // Not mapped
        Assert.Equal(0, dto.QuantityInStorage); // Not mapped
        Assert.Equal(0, dto.QuantityOnShelf); // Not mapped
    }

    [Fact]
    public void ToDtoWithStock_ProductBatch_MapsStockQuantitiesAndProductName()
    {
        // Arrange
        var product = new Product { Id = 10, Name = "Test Product", CreatedAt = DateTime.UtcNow };
        var batch = new ProductBatch
        {
            Id = 1,
            ProductId = 10,
            BestBeforeDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            Product = product
        };

        // Act
        var dto = batch.ToDtoWithStock(quantityInStorage: 50, quantityOnShelf: 25);

        // Assert
        Assert.Equal("Test Product", dto.ProductName);
        Assert.Equal(50, dto.QuantityInStorage);
        Assert.Equal(25, dto.QuantityOnShelf);
        Assert.Equal(1, dto.Id);
    }

    [Fact]
    public void ToDtoWithStock_ProductBatch_WithZeroQuantities()
    {
        // Arrange
        var product = new Product { Id = 10, Name = "Empty Product", CreatedAt = DateTime.UtcNow };
        var batch = new ProductBatch
        {
            Id = 1,
            ProductId = 10,
            BestBeforeDate = DateTime.UtcNow.AddMonths(6),
            CreatedAt = DateTime.UtcNow,
            Product = product
        };

        // Act
        var dto = batch.ToDtoWithStock(quantityInStorage: 0, quantityOnShelf: 0);

        // Assert
        Assert.Equal(0, dto.QuantityInStorage);
        Assert.Equal(0, dto.QuantityOnShelf);
    }

    [Fact]
    public void ToEntity_CreateProductBatchDto_CreatesEntityWithCorrectValues()
    {
        // Arrange
        var dto = new CreateProductBatchDto
        {
            ProductId = 10,
            BestBeforeDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            InitialQuantity = 100
        };

        // Act
        var batch = dto.ToEntity();

        // Assert
        Assert.Equal(10, batch.ProductId);
        Assert.Equal(new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc), batch.BestBeforeDate);
        Assert.True((DateTime.UtcNow - batch.CreatedAt).TotalSeconds < 5); // Created recently
    }

    [Fact]
    public void ToDtoWithStock_ProductBatch_WithNullProduct_ReturnsNullProductName()
    {
        // Arrange
        var batch = new ProductBatch
        {
            Id = 1,
            ProductId = 10,
            BestBeforeDate = DateTime.UtcNow.AddMonths(6),
            CreatedAt = DateTime.UtcNow,
            Product = null!
        };

        // Act
        var dto = batch.ToDtoWithStock(quantityInStorage: 50, quantityOnShelf: 25);

        // Assert
        Assert.Null(dto.ProductName);
        Assert.Equal(50, dto.QuantityInStorage);
        Assert.Equal(25, dto.QuantityOnShelf);
    }
}
