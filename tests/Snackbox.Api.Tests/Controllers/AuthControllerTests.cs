using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Snackbox.Api.Controllers;
using Snackbox.Api.Data;
using Snackbox.Api.DTOs;
using Snackbox.Api.Models;
using Snackbox.Api.Services;
using Xunit;

namespace Snackbox.Api.Tests.Controllers;

public class AuthControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AuthenticationService _authService;
    private readonly AuthController _controller;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly string _testPassword = "TestPassword123";
    private readonly string _testBarcode = "TEST-BARCODE";

    public AuthControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        var configurationMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        var configurationSectionMock = new Mock<Microsoft.Extensions.Configuration.IConfigurationSection>();

        configurationSectionMock.Setup(s => s["SecretKey"]).Returns("ThisIsAVeryLongKeyForTestingPurposes12345!");
        configurationSectionMock.Setup(s => s["Issuer"]).Returns("Snackbox.Api");
        configurationSectionMock.Setup(s => s["Audience"]).Returns("Snackbox.Web");
        configurationSectionMock.Setup(s => s["ExpirationMinutes"]).Returns("60");

        configurationMock.Setup(c => c.GetSection("JwtSettings")).Returns(configurationSectionMock.Object);

        _authService = new AuthenticationService(_context, configurationMock.Object);
        _loggerMock = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(_authService, _loggerMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(_testPassword),
            IsAdmin = false,
            CreatedAt = DateTime.UtcNow
        };

        var barcode = new Barcode
        {
            Id = 1,
            UserId = 1,
            Code = _testBarcode,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.Barcodes.Add(barcode);
        _context.SaveChanges();
    }

    [Fact]
    public async Task Login_WithBarcodeAndPassword_ReturnsOk()
    {
        // Arrange
        var request = new LoginRequest
        {
            BarcodeValue = _testBarcode,
            Password = _testPassword
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<LoginResponse>(okResult.Value);
        Assert.Equal("testuser", response.Username);
        Assert.NotNull(response.Token);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            BarcodeValue = _testBarcode,
            Password = "WrongPassword"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_WithNonExistentBarcode_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            BarcodeValue = "NON-EXISTENT",
            Password = _testPassword
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
