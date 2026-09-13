using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Snackbox.Api.Controllers;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Snackbox.Api.Services;
using Xunit;

namespace Snackbox.Api.Tests.Controllers;

public class TestHelperControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public TestHelperControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _context.Users.Add(new User { Id = 1, Username = "Admin", IsAdmin = true, CreatedAt = DateTime.UtcNow });
        _context.Users.Add(new User { Id = 2, Username = "Regular", CreatedAt = DateTime.UtcNow });
        _context.Users.Add(new User { Id = 3, Username = "Retired", IsRetired = true, CreatedAt = DateTime.UtcNow });
        _context.Barcodes.Add(new PurchaseBarcode { Id = 1, UserId = 2, Code = "REG-50", Amount = 0.50m, CreatedAt = DateTime.UtcNow });
        _context.Barcodes.Add(new LoginBarcode { Id = 2, UserId = 2, Code = "REG-LOGIN", Amount = 0m, CreatedAt = DateTime.UtcNow });
        _context.SaveChanges();
    }

    private TestHelperController CreateController(bool enabled = true, string environment = "Development")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TestHelper:Enabled", enabled.ToString() },
                { "JwtSettings:SecretKey", "test-secret-key-that-is-long-enough-for-hmac-sha256!" }
            })
            .Build();

        var env = new Mock<IHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns(environment);

        var authService = new AuthenticationService(_context, config);
        var seeder = new DatabaseSeeder(_context, NullLogger<DatabaseSeeder>.Instance);
        return new TestHelperController(_context, config, env.Object, authService, seeder, NullLogger<TestHelperController>.Instance);
    }

    [Fact]
    public async Task GetUsers_Enabled_ReturnsUsersWithBarcodesExcludingRetired()
    {
        var result = await CreateController().GetUsers();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsType<List<TestUserDto>>(ok.Value);
        Assert.Equal(2, users.Count);
        Assert.DoesNotContain(users, u => u.Username == "Retired");

        var regular = users.Single(u => u.Username == "Regular");
        Assert.Equal(2, regular.Barcodes.Count);
        Assert.Single(regular.Barcodes, b => b.IsLoginBarcode);
        Assert.Contains(regular.Barcodes, b => b is { Code: "REG-50", Amount: 0.50m, IsLoginBarcode: false });
    }

    [Fact]
    public async Task GetUsers_DisabledByConfig_Returns404()
    {
        var result = await CreateController(enabled: false).GetUsers();
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetUsers_NonDevelopmentEnvironment_Returns404()
    {
        var result = await CreateController(environment: "Production").GetUsers();
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Login_Enabled_ReturnsTokenWithoutPassword()
    {
        var result = await CreateController().Login(new TestLoginRequest { UserId = 1 });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<LoginResponse>(ok.Value);
        Assert.Equal("Admin", response.Username);
        Assert.True(response.IsAdmin);
        Assert.False(string.IsNullOrEmpty(response.Token));
    }

    [Fact]
    public async Task Login_UnknownUser_Returns404()
    {
        var result = await CreateController().Login(new TestLoginRequest { UserId = 999 });
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_NonDevelopmentEnvironment_Returns404()
    {
        var result = await CreateController(environment: "Production").Login(new TestLoginRequest { UserId = 1 });
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task ResetDatabase_Enabled_WipesAndReseeds()
    {
        var result = await CreateController().ResetDatabase();

        Assert.IsType<OkObjectResult>(result);
        // Original test users are gone, seeder's sample users are there instead
        var usernames = await _context.Users.Select(u => u.Username).ToListAsync();
        Assert.DoesNotContain("Regular", usernames);
        Assert.Contains("admin", usernames);
    }

    [Fact]
    public async Task ResetDatabase_NonDevelopmentEnvironment_Returns404()
    {
        var result = await CreateController(environment: "Production").ResetDatabase();

        Assert.IsType<NotFoundResult>(result);
        Assert.Contains(await _context.Users.Select(u => u.Username).ToListAsync(), u => u == "Regular");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
