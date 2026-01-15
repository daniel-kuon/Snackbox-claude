using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Snackbox.Api.Controllers;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Xunit;

namespace Snackbox.Api.Tests.Controllers;

public class BarcodesControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly BarcodesController _controller;

    public BarcodesControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<BarcodesController>>();
        _controller = new BarcodesController(_context, logger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var user1 = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            IsAdmin = false,
            CreatedAt = DateTime.UtcNow
        };

        var user2 = new User
        {
            Id = 2,
            Username = "admin",
            Email = "admin@example.com",
            IsAdmin = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.AddRange(user1, user2);

        var barcode1 = new Barcode
        {
            Id = 1,
            UserId = 1,
            Code = "USER1BARCODE1",
            Amount = 5.0m,
            IsActive = true,
            IsLoginOnly = false,
            CreatedAt = DateTime.UtcNow
        };

        var barcode2 = new Barcode
        {
            Id = 2,
            UserId = 1,
            Code = "USER1BARCODE2",
            Amount = 10.0m,
            IsActive = true,
            IsLoginOnly = false,
            CreatedAt = DateTime.UtcNow
        };

        var barcode3 = new Barcode
        {
            Id = 3,
            UserId = 1,
            Code = "USER1LOGIN",
            Amount = 0.0m,
            IsActive = true,
            IsLoginOnly = true,
            CreatedAt = DateTime.UtcNow
        };

        var barcode4 = new Barcode
        {
            Id = 4,
            UserId = 2,
            Code = "USER2BARCODE1",
            Amount = 15.0m,
            IsActive = true,
            IsLoginOnly = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Barcodes.AddRange(barcode1, barcode2, barcode3, barcode4);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetMyBarcodes_ReturnsOnlyActiveNonLoginBarcodesForCurrentUser()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _controller.GetMyBarcodes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var barcodes = Assert.IsAssignableFrom<IEnumerable<BarcodeDto>>(okResult.Value);
        var barcodeList = barcodes.ToList();
        
        // Should only return 2 barcodes (excluding login-only barcode)
        Assert.Equal(2, barcodeList.Count);
        Assert.All(barcodeList, b => Assert.False(b.IsLoginOnly));
        Assert.All(barcodeList, b => Assert.True(b.IsActive));
        Assert.All(barcodeList, b => Assert.Equal(1, b.UserId));
    }

    [Fact]
    public async Task GetMyBarcodes_ReturnsUnauthorized_WhenNoUserIdClaim()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _controller.GetMyBarcodes();

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
