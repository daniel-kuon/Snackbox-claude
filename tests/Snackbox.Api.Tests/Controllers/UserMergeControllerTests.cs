using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Snackbox.Api.Controllers;
using Snackbox.Api.Data;
using Snackbox.Api.Models;
using Xunit;

namespace Snackbox.Api.Tests.Controllers;

public class UserMergeControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserMergeController _controller;

    public UserMergeControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _controller = new UserMergeController(_context, NullLogger<UserMergeController>.Instance);

        var now = DateTime.UtcNow;
        // Jane lost her card and set up card 7 as "Jane 2" before telling the admin
        _context.Users.AddRange(
            new User { Id = 1, Username = "Jane", Email = "jane@example.com", CreatedAt = now },
            new User { Id = 2, Username = "Jane 2", Email = "jane2@example.com", CardNumber = 7, CreatedAt = now },
            new User { Id = 3, Username = "Admin", IsAdmin = true, CreatedAt = now });
        _context.Barcodes.Add(new PurchaseBarcode { Id = 1, UserId = 2, Code = "NEW-50", Amount = 0.50m, CreatedAt = now });
        _context.Purchases.Add(new Purchase { Id = 1, UserId = 2, CreatedAt = now, UpdatedAt = now });
        _context.Payments.Add(new Payment { Id = 1, UserId = 2, Amount = 5m, PaidAt = now });
        _context.UserAchievements.AddRange(
            new UserAchievement { Id = 1, UserId = 1, AchievementId = 10, EarnedAt = now },
            new UserAchievement { Id = 2, UserId = 2, AchievementId = 10, EarnedAt = now },
            new UserAchievement { Id = 3, UserId = 2, AchievementId = 11, EarnedAt = now });
        _context.SaveChanges();
    }

    [Fact]
    public async Task MergeInto_MovesEverythingAndDeletesTheCardUser()
    {
        var result = await _controller.MergeInto(2, 1);

        Assert.IsType<OkObjectResult>(result);
        Assert.Null(await _context.Users.FindAsync(2));
        Assert.Equal(1, (await _context.Barcodes.SingleAsync(b => b.Code == "NEW-50")).UserId);
        Assert.Equal(1, (await _context.Purchases.SingleAsync()).UserId);
        Assert.Equal(1, (await _context.Payments.SingleAsync()).UserId);
        Assert.Equal(7, (await _context.Users.FindAsync(1))!.CardNumber);

        // Achievement 10 she already had; 11 is new to her
        var achievements = await _context.UserAchievements.Where(a => a.UserId == 1).Select(a => a.AchievementId).ToListAsync();
        Assert.Equal(new[] { 10, 11 }, achievements.Order());
        Assert.Equal(2, await _context.UserAchievements.CountAsync());
    }

    [Fact]
    public async Task MergeInto_AdminSource_IsRefused()
    {
        var result = await _controller.MergeInto(3, 1);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(await _context.Users.FindAsync(3));
    }

    [Fact]
    public async Task MergeInto_Itself_IsRefused()
    {
        Assert.IsType<BadRequestObjectResult>(await _controller.MergeInto(1, 1));
    }

    public void Dispose() => _context.Dispose();
}
