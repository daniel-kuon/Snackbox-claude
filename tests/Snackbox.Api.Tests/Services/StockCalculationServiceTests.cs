using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Snackbox.Api.Services;
using Xunit;

namespace Snackbox.Api.Tests.Services;

public class StockCalculationServiceTests
{
    private readonly StockCalculationService _service;

    public StockCalculationServiceTests()
    {
        _service = new StockCalculationService();
    }

    [Fact]
    public void CalculateAverageProductsShelvedPerWeek_NoActions_ReturnsZero()
    {
        // Arrange
        var batches = new List<ProductBatch>();

        // Act
        var result = _service.CalculateAverageProductsShelvedPerWeek(batches);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateAverageProductsShelvedPerWeek_SingleDayActions_ReturnsAverageBasedOnOneWeek()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var batch = new ProductBatch
        {
            Id = 1,
            ProductId = 1,
            BestBeforeDate = now.AddMonths(6),
            CreatedAt = now,
            ShelvingActions = new List<ShelvingAction>
            {
                new ShelvingAction
                {
                    Id = 1,
                    ProductBatchId = 1,
                    Quantity = 50,
                    Type = ShelvingActionType.AddedToShelf,
                    ActionAt = now
                },
                new ShelvingAction
                {
                    Id = 2,
                    ProductBatchId = 1,
                    Quantity = 30,
                    Type = ShelvingActionType.MovedToShelf,
                    ActionAt = now
                }
            }
        };

        // Act
        var result = _service.CalculateAverageProductsShelvedPerWeek(new[] { batch });

        // Assert
        Assert.Equal(80, result); // 80 items / 1 week = 80
    }

    [Fact]
    public void CalculateAverageProductsShelvedPerWeek_MultiWeekActions_ReturnsCorrectAverage()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var batch = new ProductBatch
        {
            Id = 1,
            ProductId = 1,
            BestBeforeDate = now.AddMonths(6),
            CreatedAt = now,
            ShelvingActions = new List<ShelvingAction>
            {
                new ShelvingAction
                {
                    Id = 1,
                    ProductBatchId = 1,
                    Quantity = 50,
                    Type = ShelvingActionType.AddedToShelf,
                    ActionAt = now.AddDays(-21) // 3 weeks ago
                },
                new ShelvingAction
                {
                    Id = 2,
                    ProductBatchId = 1,
                    Quantity = 30,
                    Type = ShelvingActionType.MovedToShelf,
                    ActionAt = now.AddDays(-14) // 2 weeks ago
                },
                new ShelvingAction
                {
                    Id = 3,
                    ProductBatchId = 1,
                    Quantity = 20,
                    Type = ShelvingActionType.AddedToShelf,
                    ActionAt = now // now
                }
            }
        };

        // Act
        var result = _service.CalculateAverageProductsShelvedPerWeek(new[] { batch });

        // Assert
        // 100 items over 3 weeks = 33.33... per week
        Assert.True(result >= 33.0 && result <= 34.0);
    }

    [Fact]
    public void CalculateAverageProductsShelvedPerWeek_IgnoresNonShelfActions()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var batch = new ProductBatch
        {
            Id = 1,
            ProductId = 1,
            BestBeforeDate = now.AddMonths(6),
            CreatedAt = now,
            ShelvingActions = new List<ShelvingAction>
            {
                new ShelvingAction
                {
                    Id = 1,
                    ProductBatchId = 1,
                    Quantity = 50,
                    Type = ShelvingActionType.AddedToShelf,
                    ActionAt = now
                },
                new ShelvingAction
                {
                    Id = 2,
                    ProductBatchId = 1,
                    Quantity = 100,
                    Type = ShelvingActionType.AddedToStorage, // Should be ignored
                    ActionAt = now
                },
                new ShelvingAction
                {
                    Id = 3,
                    ProductBatchId = 1,
                    Quantity = 10,
                    Type = ShelvingActionType.RemovedFromShelf, // Should be ignored
                    ActionAt = now
                }
            }
        };

        // Act
        var result = _service.CalculateAverageProductsShelvedPerWeek(new[] { batch });

        // Assert
        Assert.Equal(50, result); // Only 50 from AddedToShelf
    }

    [Fact]
    public void GetEarliestBestBeforeDateInStorage_NoBatchesWithStock_ReturnsNull()
    {
        // Arrange
        var batches = new List<ProductBatch>();

        // Act
        var result = _service.GetEarliestBestBeforeDateInStorage(batches);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetEarliestBestBeforeDateInStorage_SingleBatchWithStock_ReturnsDate()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var batch = new ProductBatch
        {
            Id = 1,
            ProductId = 1,
            BestBeforeDate = now.AddMonths(6).Date,
            CreatedAt = now,
            ShelvingActions = new List<ShelvingAction>
            {
                new ShelvingAction
                {
                    Id = 1,
                    ProductBatchId = 1,
                    Quantity = 50,
                    Type = ShelvingActionType.AddedToStorage,
                    ActionAt = now
                }
            }
        };

        // Act
        var result = _service.GetEarliestBestBeforeDateInStorage(new[] { batch });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(now.AddMonths(6).Date, result.Value);
    }

    [Fact]
    public void GetEarliestBestBeforeDateInStorage_MultipleBatches_ReturnsEarliest()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var batches = new List<ProductBatch>
        {
            new ProductBatch
            {
                Id = 1,
                ProductId = 1,
                BestBeforeDate = now.AddMonths(6).Date,
                CreatedAt = now,
                ShelvingActions = new List<ShelvingAction>
                {
                    new ShelvingAction
                    {
                        Id = 1,
                        ProductBatchId = 1,
                        Quantity = 50,
                        Type = ShelvingActionType.AddedToStorage,
                        ActionAt = now
                    }
                }
            },
            new ProductBatch
            {
                Id = 2,
                ProductId = 1,
                BestBeforeDate = now.AddMonths(3).Date, // Earlier date
                CreatedAt = now,
                ShelvingActions = new List<ShelvingAction>
                {
                    new ShelvingAction
                    {
                        Id = 2,
                        ProductBatchId = 2,
                        Quantity = 30,
                        Type = ShelvingActionType.AddedToStorage,
                        ActionAt = now
                    }
                }
            },
            new ProductBatch
            {
                Id = 3,
                ProductId = 1,
                BestBeforeDate = now.AddMonths(9).Date,
                CreatedAt = now,
                ShelvingActions = new List<ShelvingAction>
                {
                    new ShelvingAction
                    {
                        Id = 3,
                        ProductBatchId = 3,
                        Quantity = 20,
                        Type = ShelvingActionType.AddedToStorage,
                        ActionAt = now
                    }
                }
            }
        };

        // Act
        var result = _service.GetEarliestBestBeforeDateInStorage(batches);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(now.AddMonths(3).Date, result.Value);
    }

    [Fact]
    public void GetEarliestBestBeforeDateInStorage_IgnoresBatchesWithNoStock()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var batches = new List<ProductBatch>
        {
            new ProductBatch
            {
                Id = 1,
                ProductId = 1,
                BestBeforeDate = now.AddMonths(3).Date,
                CreatedAt = now,
                ShelvingActions = new List<ShelvingAction>
                {
                    new ShelvingAction
                    {
                        Id = 1,
                        ProductBatchId = 1,
                        Quantity = 50,
                        Type = ShelvingActionType.AddedToStorage,
                        ActionAt = now
                    },
                    new ShelvingAction
                    {
                        Id = 2,
                        ProductBatchId = 1,
                        Quantity = 50,
                        Type = ShelvingActionType.MovedToShelf, // Remove all from storage
                        ActionAt = now
                    }
                }
            },
            new ProductBatch
            {
                Id = 2,
                ProductId = 1,
                BestBeforeDate = now.AddMonths(6).Date,
                CreatedAt = now,
                ShelvingActions = new List<ShelvingAction>
                {
                    new ShelvingAction
                    {
                        Id = 3,
                        ProductBatchId = 2,
                        Quantity = 30,
                        Type = ShelvingActionType.AddedToStorage,
                        ActionAt = now
                    }
                }
            }
        };

        // Act
        var result = _service.GetEarliestBestBeforeDateInStorage(batches);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(now.AddMonths(6).Date, result.Value); // Should return batch 2, not batch 1
    }

    [Fact]
    public void GetEarliestBestBeforeDateOnShelf_ReturnsCorrectDate()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var batches = new List<ProductBatch>
        {
            new ProductBatch
            {
                Id = 1,
                ProductId = 1,
                BestBeforeDate = now.AddMonths(3).Date,
                CreatedAt = now,
                ShelvingActions = new List<ShelvingAction>
                {
                    new ShelvingAction
                    {
                        Id = 1,
                        ProductBatchId = 1,
                        Quantity = 50,
                        Type = ShelvingActionType.AddedToShelf,
                        ActionAt = now
                    }
                }
            },
            new ProductBatch
            {
                Id = 2,
                ProductId = 1,
                BestBeforeDate = now.AddMonths(6).Date,
                CreatedAt = now,
                ShelvingActions = new List<ShelvingAction>
                {
                    new ShelvingAction
                    {
                        Id = 2,
                        ProductBatchId = 2,
                        Quantity = 30,
                        Type = ShelvingActionType.MovedToShelf,
                        ActionAt = now
                    }
                }
            }
        };

        // Act
        var result = _service.GetEarliestBestBeforeDateOnShelf(batches);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(now.AddMonths(3).Date, result.Value);
    }
}
