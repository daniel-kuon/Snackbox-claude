using Snackbox.Api.Models;
using Snackbox.Api.Services;
using Xunit;

namespace Snackbox.Api.Tests.Services;

public class BalanceCalculationServiceTests
{
    private readonly BalanceCalculationService _service;

    public BalanceCalculationServiceTests()
    {
        _service = new BalanceCalculationService();
    }

    [Fact]
    public void CalculateBalance_WithUserEntity_ReturnsCorrectBalance()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@test.com",
            Payments = new List<Payment>
            {
                new() { Amount = 100.00m },
                new() { Amount = 50.00m }
            },
            Purchases = new List<Purchase>
            {
                new()
                {
                    Scans = new List<BarcodeScan>
                    {
                        new() { Amount = 5.00m },
                        new() { Amount = 10.00m }
                    }
                },
                new()
                {
                    ManualAmount = 20.00m,
                    Scans = new List<BarcodeScan>()
                }
            },
            Withdrawals = new List<Withdrawal>
            {
                new() { Amount = 30.00m }
            }
        };

        // Act
        var balance = _service.CalculateBalance(user);

        // Assert
        // Balance = 150 (payments) - 35 (purchases) - 30 (withdrawals) = 85
        Assert.Equal(85.00m, balance);
    }

    [Fact]
    public void CalculateBalance_WithCollections_ReturnsCorrectBalance()
    {
        // Arrange
        var payments = new List<Payment>
        {
            new() { Amount = 100.00m },
            new() { Amount = 50.00m }
        };

        var purchases = new List<Purchase>
        {
            new()
            {
                Scans = new List<BarcodeScan>
                {
                    new() { Amount = 5.00m },
                    new() { Amount = 10.00m }
                }
            },
            new()
            {
                ManualAmount = 20.00m,
                Scans = new List<BarcodeScan>()
            }
        };

        var withdrawals = new List<Withdrawal>
        {
            new() { Amount = 30.00m }
        };

        // Act
        var balance = _service.CalculateBalance(payments, purchases, withdrawals);

        // Assert
        // Balance = 150 (payments) - 35 (purchases) - 30 (withdrawals) = 85
        Assert.Equal(85.00m, balance);
    }

    [Fact]
    public void CalculateBalance_NoPayments_ReturnsNegativeBalance()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@test.com",
            Payments = new List<Payment>(),
            Purchases = new List<Purchase>
            {
                new()
                {
                    Scans = new List<BarcodeScan>
                    {
                        new() { Amount = 25.00m }
                    }
                }
            },
            Withdrawals = new List<Withdrawal>()
        };

        // Act
        var balance = _service.CalculateBalance(user);

        // Assert - User owes 25
        Assert.Equal(-25.00m, balance);
    }

    [Fact]
    public void CalculateBalance_NoPurchases_ReturnsPositiveBalance()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@test.com",
            Payments = new List<Payment>
            {
                new() { Amount = 100.00m }
            },
            Purchases = new List<Purchase>(),
            Withdrawals = new List<Withdrawal>()
        };

        // Act
        var balance = _service.CalculateBalance(user);

        // Assert - User has 100 credit
        Assert.Equal(100.00m, balance);
    }

    [Fact]
    public void CalculateBalance_EmptyCollections_ReturnsZero()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@test.com",
            Payments = new List<Payment>(),
            Purchases = new List<Purchase>(),
            Withdrawals = new List<Withdrawal>()
        };

        // Act
        var balance = _service.CalculateBalance(user);

        // Assert
        Assert.Equal(0m, balance);
    }

    [Fact]
    public void CalculateBalance_NullCollections_ReturnsZero()
    {
        // Arrange - Using collection overload with nulls
        List<Payment>? payments = null;
        List<Purchase>? purchases = null;
        List<Withdrawal>? withdrawals = null;

        // Act
        var balance = _service.CalculateBalance(payments!, purchases!, withdrawals!);

        // Assert
        Assert.Equal(0m, balance);
    }

    [Fact]
    public void CalculateBalance_WithWithdrawals_SubtractsFromBalance()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@test.com",
            Payments = new List<Payment>
            {
                new() { Amount = 100.00m }
            },
            Purchases = new List<Purchase>
            {
                new()
                {
                    Scans = new List<BarcodeScan>
                    {
                        new() { Amount = 20.00m }
                    }
                }
            },
            Withdrawals = new List<Withdrawal>
            {
                new() { Amount = 50.00m }
            }
        };

        // Act
        var balance = _service.CalculateBalance(user);

        // Assert
        // Balance = 100 (payments) - 20 (purchases) - 50 (withdrawals) = 30
        Assert.Equal(30.00m, balance);
    }

    [Fact]
    public void CalculateBalance_ManualAmountTakesPrecedenceOverScans()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@test.com",
            Payments = new List<Payment>
            {
                new() { Amount = 100.00m }
            },
            Purchases = new List<Purchase>
            {
                new()
                {
                    ManualAmount = 25.00m,
                    Scans = new List<BarcodeScan>
                    {
                        // These should be ignored when ManualAmount is set
                        new() { Amount = 10.00m },
                        new() { Amount = 10.00m }
                    }
                }
            },
            Withdrawals = new List<Withdrawal>()
        };

        // Act
        var balance = _service.CalculateBalance(user);

        // Assert
        // Balance = 100 (payments) - 25 (manual amount, not 20 from scans) = 75
        Assert.Equal(75.00m, balance);
    }

    [Fact]
    public void CalculateBalance_NullUser_ThrowsArgumentNullException()
    {
        // Arrange
        User? user = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.CalculateBalance(user!));
    }

    [Fact]
    public void CalculateBalance_ComplexScenario_ReturnsCorrectBalance()
    {
        // Arrange - Real-world scenario
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@test.com",
            Payments = new List<Payment>
            {
                new() { Amount = 200.00m },
                new() { Amount = 150.00m },
                new() { Amount = 75.00m }
            },
            Purchases = new List<Purchase>
            {
                // Regular purchase with scans
                new()
                {
                    Scans = new List<BarcodeScan>
                    {
                        new() { Amount = 2.50m },
                        new() { Amount = 3.75m },
                        new() { Amount = 1.25m }
                    }
                },
                // Manual correction
                new()
                {
                    ManualAmount = -5.00m, // Refund
                    Scans = new List<BarcodeScan>()
                },
                // Another regular purchase
                new()
                {
                    Scans = new List<BarcodeScan>
                    {
                        new() { Amount = 10.00m }
                    }
                }
            },
            Withdrawals = new List<Withdrawal>
            {
                new() { Amount = 100.00m },
                new() { Amount = 50.00m }
            }
        };

        // Act
        var balance = _service.CalculateBalance(user);

        // Assert
        // Payments: 200 + 150 + 75 = 425
        // Purchases: 2.50 + 3.75 + 1.25 + (-5.00) + 10.00 = 12.50
        // Withdrawals: 100 + 50 = 150
        // Balance = 425 - 12.50 - 150 = 262.50
        Assert.Equal(262.50m, balance);
    }
}
