using Snackbox.Api.DTOs;
using Snackbox.Api.Mappers;
using Snackbox.Api.Models;
using Xunit;

namespace Snackbox.Api.Tests.Mappers;

public class PaymentMapperTests
{
    [Fact]
    public void ToDto_Payment_MapsAllProperties()
    {
        // Arrange
        var payment = new Payment
        {
            Id = 1,
            UserId = 10,
            Amount = 50.00m,
            PaidAt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            Notes = "Test payment"
        };

        // Act
        var dto = payment.ToDto();

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal(10, dto.UserId);
        Assert.Equal(50.00m, dto.Amount);
        Assert.Equal(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc), dto.PaidAt);
        Assert.Equal("Test payment", dto.Notes);
        Assert.Null(dto.Username); // Not mapped by ToDto
    }

    [Fact]
    public void ToDtoWithUser_Payment_MapsUsernameFromNavigationProperty()
    {
        // Arrange
        var user = new User { Id = 10, Username = "testuser", CreatedAt = DateTime.UtcNow };
        var payment = new Payment
        {
            Id = 1,
            UserId = 10,
            Amount = 50.00m,
            PaidAt = DateTime.UtcNow,
            Notes = "Test payment",
            User = user
        };

        // Act
        var dto = payment.ToDtoWithUser();

        // Assert
        Assert.Equal("testuser", dto.Username);
        Assert.Equal(1, dto.Id);
        Assert.Equal(50.00m, dto.Amount);
    }

    [Fact]
    public void ToDtoListWithUser_Payments_MapsAllItemsWithUsernames()
    {
        // Arrange
        var user1 = new User { Id = 1, Username = "user1", CreatedAt = DateTime.UtcNow };
        var user2 = new User { Id = 2, Username = "user2", CreatedAt = DateTime.UtcNow };
        var payments = new List<Payment>
        {
            new Payment { Id = 1, UserId = 1, Amount = 10m, PaidAt = DateTime.UtcNow, User = user1 },
            new Payment { Id = 2, UserId = 2, Amount = 20m, PaidAt = DateTime.UtcNow, User = user2 }
        };

        // Act
        var dtos = payments.ToDtoListWithUser();

        // Assert
        Assert.Equal(2, dtos.Count);
        Assert.Equal("user1", dtos[0].Username);
        Assert.Equal("user2", dtos[1].Username);
    }

    [Fact]
    public void ToEntity_CreatePaymentDto_CreatesPaymentWithCorrectValues()
    {
        // Arrange
        var dto = new CreatePaymentDto
        {
            UserId = 10,
            Amount = 100.00m,
            Notes = "Monthly payment"
        };

        // Act
        var payment = dto.ToEntity();

        // Assert
        Assert.Equal(10, payment.UserId);
        Assert.Equal(100.00m, payment.Amount);
        Assert.Equal("Monthly payment", payment.Notes);
        Assert.True((DateTime.UtcNow - payment.PaidAt).TotalSeconds < 5); // Created recently
    }

    [Fact]
    public void ToEntity_CreatePaymentDto_WithNullNotes_SetsNullNotes()
    {
        // Arrange
        var dto = new CreatePaymentDto
        {
            UserId = 10,
            Amount = 100.00m,
            Notes = null
        };

        // Act
        var payment = dto.ToEntity();

        // Assert
        Assert.Null(payment.Notes);
    }
}
