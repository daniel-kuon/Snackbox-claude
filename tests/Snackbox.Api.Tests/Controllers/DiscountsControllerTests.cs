using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Controllers;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Xunit;

namespace Snackbox.Api.Tests.Controllers;

public class DiscountsControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DiscountsController _controller;

    public DiscountsControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _controller = new DiscountsController(_context);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var discount1 = new Discount
        {
            Id = 1,
            Name = "10% Off",
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(7),
            MinimumPurchaseAmount = 5.00m,
            Type = DiscountType.Percentage,
            Value = 10m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var discount2 = new Discount
        {
            Id = 2,
            Name = "50 Cent Off",
            ValidFrom = DateTime.UtcNow.AddDays(-5),
            ValidTo = DateTime.UtcNow.AddDays(2),
            MinimumPurchaseAmount = 3.00m,
            Type = DiscountType.FixedAmount,
            Value = 0.50m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-6)
        };

        var discount3 = new Discount
        {
            Id = 3,
            Name = "Expired Discount",
            ValidFrom = DateTime.UtcNow.AddDays(-10),
            ValidTo = DateTime.UtcNow.AddDays(-3),
            MinimumPurchaseAmount = 2.00m,
            Type = DiscountType.Percentage,
            Value = 20m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-11)
        };

        _context.Discounts.AddRange(discount1, discount2, discount3);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetDiscounts_ReturnsAllDiscounts()
    {
        // Act
        var result = await _controller.GetDiscounts();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var discounts = Assert.IsType<List<DiscountDto>>(okResult.Value);
        Assert.Equal(3, discounts.Count);
    }

    [Fact]
    public async Task GetDiscounts_WithActiveOnlyFilter_ReturnsOnlyActiveDiscounts()
    {
        // Act
        var result = await _controller.GetDiscounts(activeOnly: true);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var discounts = Assert.IsType<List<DiscountDto>>(okResult.Value);
        Assert.Equal(2, discounts.Count); // Only discount1 and discount2 are active and not expired
    }

    [Fact]
    public async Task GetDiscount_WithValidId_ReturnsDiscount()
    {
        // Act
        var result = await _controller.GetDiscount(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var discount = Assert.IsType<DiscountDto>(okResult.Value);
        Assert.Equal("10% Off", discount.Name);
        Assert.Equal("Percentage", discount.Type);
    }

    [Fact]
    public async Task GetDiscount_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetDiscount(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateDiscount_WithValidData_CreatesDiscount()
    {
        // Arrange
        var newDiscount = new DiscountDto
        {
            Name = "New Discount",
            ValidFrom = DateTime.UtcNow,
            ValidTo = DateTime.UtcNow.AddDays(10),
            MinimumPurchaseAmount = 5.00m,
            Type = "FixedAmount",
            Value = 1.00m,
            IsActive = true
        };

        // Act
        var result = await _controller.CreateDiscount(newDiscount);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var discount = Assert.IsType<DiscountDto>(createdResult.Value);
        Assert.Equal("New Discount", discount.Name);
        Assert.True(discount.Id > 0);
    }

    [Fact]
    public async Task CreateDiscount_WithInvalidType_ReturnsBadRequest()
    {
        // Arrange
        var newDiscount = new DiscountDto
        {
            Name = "Invalid Discount",
            ValidFrom = DateTime.UtcNow,
            ValidTo = DateTime.UtcNow.AddDays(10),
            MinimumPurchaseAmount = 5.00m,
            Type = "InvalidType",
            Value = 10m,
            IsActive = true
        };

        // Act
        var result = await _controller.CreateDiscount(newDiscount);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid discount type. Must be 'FixedAmount' or 'Percentage'.", badRequestResult.Value);
    }

    [Fact]
    public async Task CreateDiscount_WithInvalidPercentageValue_ReturnsBadRequest()
    {
        // Arrange
        var newDiscount = new DiscountDto
        {
            Name = "Invalid Percentage",
            ValidFrom = DateTime.UtcNow,
            ValidTo = DateTime.UtcNow.AddDays(10),
            MinimumPurchaseAmount = 5.00m,
            Type = "Percentage",
            Value = 150m, // Invalid: > 100
            IsActive = true
        };

        // Act
        var result = await _controller.CreateDiscount(newDiscount);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Percentage discount must be between 0 and 100.", badRequestResult.Value);
    }

    [Fact]
    public async Task CreateDiscount_WithInvalidDateRange_ReturnsBadRequest()
    {
        // Arrange
        var newDiscount = new DiscountDto
        {
            Name = "Invalid Dates",
            ValidFrom = DateTime.UtcNow.AddDays(10),
            ValidTo = DateTime.UtcNow, // ValidTo before ValidFrom
            MinimumPurchaseAmount = 5.00m,
            Type = "FixedAmount",
            Value = 1.00m,
            IsActive = true
        };

        // Act
        var result = await _controller.CreateDiscount(newDiscount);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("ValidFrom must be before ValidTo.", badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateDiscount_WithValidData_UpdatesDiscount()
    {
        // Arrange
        var updateDto = new DiscountDto
        {
            Id = 1,
            Name = "Updated 10% Off",
            ValidFrom = DateTime.UtcNow,
            ValidTo = DateTime.UtcNow.AddDays(14),
            MinimumPurchaseAmount = 10.00m,
            Type = "Percentage",
            Value = 15m,
            IsActive = true
        };

        // Act
        var result = await _controller.UpdateDiscount(1, updateDto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var updatedDiscount = await _context.Discounts.FindAsync(1);
        Assert.NotNull(updatedDiscount);
        Assert.Equal("Updated 10% Off", updatedDiscount.Name);
        Assert.Equal(15m, updatedDiscount.Value);
    }

    [Fact]
    public async Task DeleteDiscount_WithValidId_DeletesDiscount()
    {
        // Act
        var result = await _controller.DeleteDiscount(1);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var deletedDiscount = await _context.Discounts.FindAsync(1);
        Assert.Null(deletedDiscount);
    }

    [Fact]
    public async Task DeleteDiscount_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.DeleteDiscount(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
