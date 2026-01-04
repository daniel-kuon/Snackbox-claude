using Snackbox.Api.Dtos;
using Snackbox.Api.Mappers;
using Snackbox.Api.Models;
using Xunit;

namespace Snackbox.Api.Tests.Mappers;

public class PurchaseMapperTests
{
    [Fact]
    public void ToItemDto_BarcodeScan_MapsAllPropertiesExceptProductName()
    {
        // Arrange
        var scan = new BarcodeScan
        {
            Id = 1,
            PurchaseId = 10,
            BarcodeId = 5,
            Amount = 2.50m,
            ScannedAt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
        };

        // Act
        var dto = scan.ToItemDto();

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal(2.50m, dto.Amount);
        Assert.Equal(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc), dto.ScannedAt);
        Assert.Null(dto.ProductName); // Not mapped by ToItemDto
    }

    [Fact]
    public void ToItemDtoWithProductName_BarcodeScan_MapsProductNameFromBarcode()
    {
        // Arrange
        var barcode = new Barcode { Id = 5, Code = "PROD123", UserId = 1, CreatedAt = DateTime.UtcNow };
        var scan = new BarcodeScan
        {
            Id = 1,
            PurchaseId = 10,
            BarcodeId = 5,
            Amount = 2.50m,
            ScannedAt = DateTime.UtcNow,
            Barcode = barcode
        };

        // Act
        var dto = scan.ToItemDtoWithProductName();

        // Assert
        Assert.Equal("PROD123", dto.ProductName);
        Assert.Equal(1, dto.Id);
    }

    [Fact]
    public void ToDto_Purchase_MapsAllPropertiesWithCalculatedTotal()
    {
        // Arrange
        var user = new User { Id = 10, Username = "testuser", CreatedAt = DateTime.UtcNow };
        var barcode1 = new Barcode { Id = 1, Code = "BC1", UserId = 1, CreatedAt = DateTime.UtcNow };
        var barcode2 = new Barcode { Id = 2, Code = "BC2", UserId = 1, CreatedAt = DateTime.UtcNow };
        
        var purchase = new Purchase
        {
            Id = 1,
            UserId = 10,
            CreatedAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            CompletedAt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            User = user,
            Scans = new List<BarcodeScan>
            {
                new BarcodeScan { Id = 1, Amount = 5.00m, ScannedAt = DateTime.UtcNow, Barcode = barcode1 },
                new BarcodeScan { Id = 2, Amount = 3.50m, ScannedAt = DateTime.UtcNow, Barcode = barcode2 }
            }
        };

        // Act
        var dto = purchase.ToDto();

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal(10, dto.UserId);
        Assert.Equal("testuser", dto.Username);
        Assert.Equal(8.50m, dto.TotalAmount); // 5.00 + 3.50
        Assert.Equal(new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc), dto.CreatedAt);
        Assert.Equal(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc), dto.CompletedAt);
        Assert.Equal(2, dto.Items.Count);
    }

    [Fact]
    public void ToDto_Purchase_WithNoScans_ReturnsTotalAmountZero()
    {
        // Arrange
        var user = new User { Id = 10, Username = "testuser", CreatedAt = DateTime.UtcNow };
        var purchase = new Purchase
        {
            Id = 1,
            UserId = 10,
            CreatedAt = DateTime.UtcNow,
            User = user,
            Scans = new List<BarcodeScan>()
        };

        // Act
        var dto = purchase.ToDto();

        // Assert
        Assert.Equal(0m, dto.TotalAmount);
        Assert.Empty(dto.Items);
    }

    [Fact]
    public void ToDtoList_Purchases_MapsAllItems()
    {
        // Arrange
        var user = new User { Id = 10, Username = "testuser", CreatedAt = DateTime.UtcNow };
        var purchases = new List<Purchase>
        {
            new Purchase { Id = 1, UserId = 10, CreatedAt = DateTime.UtcNow, User = user, Scans = new List<BarcodeScan>() },
            new Purchase { Id = 2, UserId = 10, CreatedAt = DateTime.UtcNow, User = user, Scans = new List<BarcodeScan>() }
        };

        // Act
        var dtos = purchases.ToDtoList();

        // Assert
        Assert.Equal(2, dtos.Count);
        Assert.Equal(1, dtos[0].Id);
        Assert.Equal(2, dtos[1].Id);
    }

    [Fact]
    public void ToDto_Purchase_WithNullUser_ReturnsNullUsername()
    {
        // Arrange
        var purchase = new Purchase
        {
            Id = 1,
            UserId = 10,
            CreatedAt = DateTime.UtcNow,
            User = null!,
            Scans = new List<BarcodeScan>()
        };

        // Act
        var dto = purchase.ToDto();

        // Assert
        Assert.Null(dto.Username);
    }
}
