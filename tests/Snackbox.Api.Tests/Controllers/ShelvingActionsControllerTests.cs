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

public class ShelvingActionsControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ShelvingActionsController _controller;

    public ShelvingActionsControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<ShelvingActionsController>>();
        var stockCalculation = new StockCalculationService();
        var bestBeforeDateService = new Mock<IProductBestBeforeDateService>();
        _controller = new ShelvingActionsController(_context, logger.Object, stockCalculation, bestBeforeDateService.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var product = new Product
        {
            Id = 1,
            Name = "Test Chips",
            Barcode = "1234567890123",
            Price = 1.50m,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);

        var batch = new ProductBatch
        {
            Id = 1,
            ProductId = 1,
            BestBeforeDate = DateTime.UtcNow.AddMonths(6).Date,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductBatches.Add(batch);

        var shelvingAction = new ShelvingAction
        {
            Id = 1,
            ProductBatchId = 1,
            Quantity = 50,
            Type = ShelvingActionType.AddedToStorage,
            ActionAt = DateTime.UtcNow
        };

        _context.ShelvingActions.Add(shelvingAction);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetAll_ReturnsAllActions()
    {
        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actions = Assert.IsType<List<ShelvingActionDto>>(okResult.Value);
        Assert.Single(actions);
    }

    [Fact]
    public async Task GetAll_WithProductFilter_ReturnsFilteredActions()
    {
        // Act
        var result = await _controller.GetAll(productId: 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actions = Assert.IsType<List<ShelvingActionDto>>(okResult.Value);
        Assert.Single(actions);
    }

    [Fact]
    public async Task GetAll_WithLimit_ReturnsLimitedActions()
    {
        // Arrange
        // Add more actions
        _context.ShelvingActions.Add(new ShelvingAction
        {
            ProductBatchId = 1,
            Quantity = 10,
            Type = ShelvingActionType.MovedToShelf,
            ActionAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(limit: 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actions = Assert.IsType<List<ShelvingActionDto>>(okResult.Value);
        Assert.Single(actions);
    }

    [Fact]
    public async Task GetById_ExistingAction_ReturnsAction()
    {
        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var action = Assert.IsType<ShelvingActionDto>(okResult.Value);
        Assert.Equal(50, action.Quantity);
        Assert.Equal("AddedToStorage", action.Type);
    }

    [Fact]
    public async Task GetById_NonExistingAction_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetById(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_ValidAction_CreatesAction()
    {
        // Arrange
        var createDto = new CreateShelvingActionDto
        {
            ProductBarcode = "1234567890123",
            BestBeforeDate = DateTime.UtcNow.AddMonths(6).Date,
            Quantity = 10,
            Type = "MovedToShelf"
        };

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var action = Assert.IsType<ShelvingActionDto>(createdResult.Value);
        Assert.Equal(10, action.Quantity);
        Assert.Equal("MovedToShelf", action.Type);
    }

    [Fact]
    public async Task Create_NewBestBeforeDate_CreatesBatchAndAction()
    {
        // Arrange
        var newDate = DateTime.UtcNow.AddMonths(12).Date;
        var createDto = new CreateShelvingActionDto
        {
            ProductBarcode = "1234567890123",
            BestBeforeDate = newDate,
            Quantity = 25,
            Type = "AddedToStorage"
        };

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var action = Assert.IsType<ShelvingActionDto>(createdResult.Value);
        Assert.Equal(25, action.Quantity);
        Assert.Equal(newDate, action.BestBeforeDate.Date);

        // Verify new batch was created
        var batches = await _context.ProductBatches.Where(b => b.ProductId == 1).ToListAsync();
        Assert.Equal(2, batches.Count);
    }

    [Fact]
    public async Task Create_NonExistingBarcode_ReturnsNotFound()
    {
        // Arrange
        var createDto = new CreateShelvingActionDto
        {
            ProductBarcode = "nonexistent",
            BestBeforeDate = DateTime.UtcNow.AddMonths(6),
            Quantity = 10,
            Type = "AddedToStorage"
        };

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_InvalidActionType_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateShelvingActionDto
        {
            ProductBarcode = "1234567890123",
            BestBeforeDate = DateTime.UtcNow.AddMonths(6),
            Quantity = 10,
            Type = "InvalidType"
        };

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_MovedToShelf_InsufficientStock_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateShelvingActionDto
        {
            ProductBarcode = "1234567890123",
            BestBeforeDate = DateTime.UtcNow.AddMonths(6).Date,
            Quantity = 100, // More than available (50)
            Type = "MovedToShelf"
        };

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateBatch_ValidActions_CreatesAllActions()
    {
        // Arrange
        var request = new BatchShelvingRequest
        {
            Actions = new List<CreateShelvingActionDto>
            {
                new() { ProductBarcode = "1234567890123", BestBeforeDate = DateTime.UtcNow.AddMonths(6).Date, Quantity = 10, Type = "MovedToShelf" }
            }
        };

        // Act
        var result = await _controller.CreateBatch(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actions = Assert.IsType<List<ShelvingActionDto>>(okResult.Value);
        Assert.Single(actions);
    }

    [Fact]
    public async Task CreateBatch_MixedValidAndInvalid_ReturnsPartialSuccess()
    {
        // Arrange
        var request = new BatchShelvingRequest
        {
            Actions = new List<CreateShelvingActionDto>
            {
                new() { ProductBarcode = "1234567890123", BestBeforeDate = DateTime.UtcNow.AddMonths(6).Date, Quantity = 5, Type = "MovedToShelf" },
                new() { ProductBarcode = "nonexistent", BestBeforeDate = DateTime.UtcNow.AddMonths(6), Quantity = 10, Type = "AddedToStorage" }
            }
        };

        // Act
        var result = await _controller.CreateBatch(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        // Should be an anonymous object with results and errors
        var value = okResult.Value;
        Assert.NotNull(value);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
