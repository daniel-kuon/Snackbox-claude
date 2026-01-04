using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Snackbox.Api.Controllers;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Snackbox.Api.Services;
using Xunit;

namespace Snackbox.Api.Tests.Controllers;

public class ProductsControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<ProductsController>>();
        var stockCalculation = new StockCalculationService();
        _controller = new ProductsController(_context, logger.Object, stockCalculation);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var product1 = new Product
        {
            Id = 1,
            Name = "Test Chips",
            Barcode = "1234567890123",
            Price = 1.50m,
            Description = "Test chips description",
            CreatedAt = DateTime.UtcNow
        };

        var product2 = new Product
        {
            Id = 2,
            Name = "Test Chocolate",
            Barcode = "1234567890124",
            Price = 2.00m,
            Description = "Test chocolate description",
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.AddRange(product1, product2);

        var batch1 = new ProductBatch
        {
            Id = 1,
            ProductId = 1,
            BestBeforeDate = DateTime.UtcNow.AddMonths(6),
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductBatches.Add(batch1);

        var shelvingAction1 = new ShelvingAction
        {
            Id = 1,
            ProductBatchId = 1,
            Quantity = 50,
            Type = ShelvingActionType.AddedToStorage,
            ActionAt = DateTime.UtcNow
        };

        var shelvingAction2 = new ShelvingAction
        {
            Id = 2,
            ProductBatchId = 1,
            Quantity = 20,
            Type = ShelvingActionType.MovedToShelf,
            ActionAt = DateTime.UtcNow
        };

        _context.ShelvingActions.AddRange(shelvingAction1, shelvingAction2);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetAll_ReturnsAllProducts()
    {
        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var products = Assert.IsType<List<ProductDto>>(okResult.Value);
        Assert.Equal(2, products.Count);
    }

    [Fact]
    public async Task GetById_ExistingProduct_ReturnsProduct()
    {
        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var product = Assert.IsType<ProductDto>(okResult.Value);
        Assert.Equal("Test Chips", product.Name);
        Assert.Equal("1234567890123", product.Barcode);
    }

    [Fact]
    public async Task GetById_NonExistingProduct_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetById(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetByBarcode_ExistingBarcode_ReturnsProduct()
    {
        // Act
        var result = await _controller.GetByBarcode("1234567890123");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var product = Assert.IsType<ProductDto>(okResult.Value);
        Assert.Equal("Test Chips", product.Name);
    }

    [Fact]
    public async Task GetByBarcode_NonExistingBarcode_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetByBarcode("nonexistent");

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetProductStock_ExistingProduct_ReturnsStockInfo()
    {
        // Act
        var result = await _controller.GetProductStock(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var stock = Assert.IsType<ProductStockDto>(okResult.Value);
        Assert.Equal(1, stock.ProductId);
        Assert.Equal("Test Chips", stock.ProductName);
        Assert.Equal(30, stock.TotalInStorage); // 50 added - 20 moved to shelf
        Assert.Equal(20, stock.TotalOnShelf);
    }

    [Fact]
    public async Task GetProductStock_NonExistingProduct_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetProductStock(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetAllProductStock_ReturnsAllProductsWithStock()
    {
        // Act
        var result = await _controller.GetAllProductStock();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var products = Assert.IsType<List<ProductStockDto>>(okResult.Value);
        Assert.Equal(2, products.Count);
        
        var chipsStock = products.First(p => p.ProductId == 1);
        Assert.Equal(30, chipsStock.TotalInStorage);
        Assert.Equal(20, chipsStock.TotalOnShelf);
    }

    [Fact]
    public async Task Create_ValidProduct_CreatesProduct()
    {
        // Arrange
        var createDto = new CreateProductDto
        {
            Name = "New Product",
            Barcode = "9999999999999",
            Price = 3.00m,
            Description = "New product description"
        };

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var product = Assert.IsType<ProductDto>(createdResult.Value);
        Assert.Equal("New Product", product.Name);
        Assert.Equal("9999999999999", product.Barcode);
    }

    [Fact]
    public async Task Create_DuplicateBarcode_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateProductDto
        {
            Name = "Duplicate Product",
            Barcode = "1234567890123", // Already exists
            Price = 3.00m
        };

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_ExistingProduct_UpdatesProduct()
    {
        // Arrange
        var updateDto = new UpdateProductDto
        {
            Name = "Updated Chips",
            Barcode = "1234567890123",
            Price = 2.00m,
            Description = "Updated description"
        };

        // Act
        var result = await _controller.Update(1, updateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var product = Assert.IsType<ProductDto>(okResult.Value);
        Assert.Equal("Updated Chips", product.Name);
        Assert.Equal(2.00m, product.Price);
    }

    [Fact]
    public async Task Delete_ProductWithBatches_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Delete(1); // Has batches

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ProductWithoutBatches_DeletesProduct()
    {
        // Act
        var result = await _controller.Delete(2); // No batches

        // Assert
        Assert.IsType<NoContentResult>(result);
        Assert.Null(await _context.Products.FindAsync(2));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
