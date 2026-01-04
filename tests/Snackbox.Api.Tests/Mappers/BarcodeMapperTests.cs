using Snackbox.Api.Dtos;
using Snackbox.Api.Mappers;
using Snackbox.Api.Models;
using Xunit;

namespace Snackbox.Api.Tests.Mappers;

public class BarcodeMapperTests
{
    [Fact]
    public void ToDto_Barcode_MapsAllProperties()
    {
        // Arrange
        var barcode = new Barcode
        {
            Id = 1,
            UserId = 10,
            Code = "BC123456",
            Amount = 5.50m,
            IsActive = true,
            IsLoginOnly = false,
            CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
        };

        // Act
        var dto = barcode.ToDto();

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal(10, dto.UserId);
        Assert.Equal("BC123456", dto.Code);
        Assert.Equal(5.50m, dto.Amount);
        Assert.True(dto.IsActive);
        Assert.False(dto.IsLoginOnly);
        Assert.Equal(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc), dto.CreatedAt);
        Assert.Null(dto.Username); // Not mapped by ToDto
    }

    [Fact]
    public void ToDtoWithUser_Barcode_MapsUsernameFromNavigationProperty()
    {
        // Arrange
        var user = new User { Id = 10, Username = "testuser", CreatedAt = DateTime.UtcNow };
        var barcode = new Barcode
        {
            Id = 1,
            UserId = 10,
            Code = "BC123456",
            Amount = 5.50m,
            IsActive = true,
            IsLoginOnly = false,
            CreatedAt = DateTime.UtcNow,
            User = user
        };

        // Act
        var dto = barcode.ToDtoWithUser();

        // Assert
        Assert.Equal("testuser", dto.Username);
        Assert.Equal(1, dto.Id);
        Assert.Equal("BC123456", dto.Code);
    }

    [Fact]
    public void ToDtoListWithUser_Barcodes_MapsAllItemsWithUsernames()
    {
        // Arrange
        var user1 = new User { Id = 1, Username = "user1", CreatedAt = DateTime.UtcNow };
        var user2 = new User { Id = 2, Username = "user2", CreatedAt = DateTime.UtcNow };
        var barcodes = new List<Barcode>
        {
            new Barcode { Id = 1, UserId = 1, Code = "BC1", Amount = 1m, IsActive = true, CreatedAt = DateTime.UtcNow, User = user1 },
            new Barcode { Id = 2, UserId = 2, Code = "BC2", Amount = 2m, IsActive = true, CreatedAt = DateTime.UtcNow, User = user2 }
        };

        // Act
        var dtos = barcodes.ToDtoListWithUser();

        // Assert
        Assert.Equal(2, dtos.Count);
        Assert.Equal("user1", dtos[0].Username);
        Assert.Equal("user2", dtos[1].Username);
    }

    [Fact]
    public void ToDtoWithUser_NullUser_ReturnsNullUsername()
    {
        // Arrange
        var barcode = new Barcode
        {
            Id = 1,
            UserId = 10,
            Code = "BC123456",
            Amount = 5.50m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            User = null!
        };

        // Act
        var dto = barcode.ToDtoWithUser();

        // Assert
        Assert.Null(dto.Username);
    }
}
