using Snackbox.Api.Dtos;
using Snackbox.Api.Mappers;
using Snackbox.Api.Models;
using Xunit;

namespace Snackbox.Api.Tests.Mappers;

public class ShelvingActionMapperTests
{
    [Fact]
    public void ToDto_ShelvingAction_MapsAllBasicProperties()
    {
        // Arrange
        var action = new ShelvingAction
        {
            Id = 1,
            ProductBatchId = 10,
            Quantity = 25,
            Type = ShelvingActionType.AddedToStorage,
            ActionAt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
        };

        // Act
        var dto = action.ToDto();

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal(10, dto.ProductBatchId);
        Assert.Equal(25, dto.Quantity);
        Assert.Equal(ShelvingActionType.AddedToStorage, dto.Type);
        Assert.Equal(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc), dto.ActionAt);
    }

    [Fact]
    public void ToDtoWithProduct_ShelvingAction_MapsProductInformationFromNavigationProperties()
    {
        // Arrange
        var product = new Product
        {
            Id = 5,
            Name = "Test Product",
            CreatedAt = DateTime.UtcNow,
            Barcodes = new List<ProductBarcode>
            {
                new ProductBarcode { Id = 1, ProductId = 5, Barcode = "123456", Quantity = 1, CreatedAt = DateTime.UtcNow }
            }
        };
        var batch = new ProductBatch
        {
            Id = 10,
            ProductId = 5,
            BestBeforeDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            Product = product
        };
        var action = new ShelvingAction
        {
            Id = 1,
            ProductBatchId = 10,
            Quantity = 25,
            Type = ShelvingActionType.MovedToShelf,
            ActionAt = DateTime.UtcNow,
            ProductBatch = batch
        };

        // Act
        var dto = action.ToDtoWithProduct();

        // Assert
        Assert.Equal(5, dto.ProductId);
        Assert.Equal("Test Product", dto.ProductName);
        Assert.Equal("123456", dto.ProductBarcode);
        Assert.Equal(new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc), dto.BestBeforeDate);
    }

    [Fact]
    public void ToDtoWithBarcode_ShelvingAction_OverridesProductBarcode()
    {
        // Arrange
        var product = new Product
        {
            Id = 5,
            Name = "Test Product",
            CreatedAt = DateTime.UtcNow,
            Barcodes = new List<ProductBarcode>
            {
                new ProductBarcode { Id = 1, ProductId = 5, Barcode = "123456", Quantity = 1, CreatedAt = DateTime.UtcNow }
            }
        };
        var batch = new ProductBatch
        {
            Id = 10,
            ProductId = 5,
            BestBeforeDate = DateTime.UtcNow.AddMonths(6),
            CreatedAt = DateTime.UtcNow,
            Product = product
        };
        var action = new ShelvingAction
        {
            Id = 1,
            ProductBatchId = 10,
            Quantity = 25,
            Type = ShelvingActionType.AddedToStorage,
            ActionAt = DateTime.UtcNow,
            ProductBatch = batch
        };

        // Act
        var dto = action.ToDtoWithBarcode("SCANNED999");

        // Assert
        Assert.Equal("SCANNED999", dto.ProductBarcode); // Overridden
        Assert.Equal("Test Product", dto.ProductName);
    }

    [Fact]
    public void ToEntity_CreateShelvingActionDto_CreatesEntityWithCorrectValues()
    {
        // Arrange
        var dto = new CreateShelvingActionDto
        {
            ProductBarcode = "123456",
            BestBeforeDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            Quantity = 50,
            Type = ShelvingActionType.AddedToStorage
        };

        // Act
        var entity = dto.ToEntity(10);

        // Assert
        Assert.Equal(10, entity.ProductBatchId);
        Assert.Equal(50, entity.Quantity);
        Assert.Equal(ShelvingActionType.AddedToStorage, entity.Type);
        Assert.True((DateTime.UtcNow - entity.ActionAt).TotalSeconds < 5); // Created recently
    }

    [Fact]
    public void ToDtoList_ShelvingActions_MapsAllItems()
    {
        // Arrange
        var product = new Product
        {
            Id = 5,
            Name = "Test Product",
            CreatedAt = DateTime.UtcNow,
            Barcodes = new List<ProductBarcode>()
        };
        var batch = new ProductBatch
        {
            Id = 10,
            ProductId = 5,
            BestBeforeDate = DateTime.UtcNow.AddMonths(6),
            CreatedAt = DateTime.UtcNow,
            Product = product
        };
        var actions = new List<ShelvingAction>
        {
            new ShelvingAction { Id = 1, ProductBatchId = 10, Quantity = 10, Type = ShelvingActionType.AddedToStorage, ActionAt = DateTime.UtcNow, ProductBatch = batch },
            new ShelvingAction { Id = 2, ProductBatchId = 10, Quantity = 5, Type = ShelvingActionType.MovedToShelf, ActionAt = DateTime.UtcNow, ProductBatch = batch }
        };

        // Act
        var dtos = actions.ToDtoList();

        // Assert
        Assert.Equal(2, dtos.Count);
        Assert.Equal(10, dtos[0].Quantity);
        Assert.Equal(5, dtos[1].Quantity);
    }
}
