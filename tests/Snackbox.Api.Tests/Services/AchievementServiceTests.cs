using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Models;
using Snackbox.Api.Services;
using Xunit;

namespace Snackbox.Api.Tests.Services;

public class AchievementServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AchievementService _service;

    public AchievementServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _service = new AchievementService(_context);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add a test user
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            IsAdmin = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);

        // Add achievements
        _context.Achievements.AddRange(
            new Achievement { Id = 1, Code = "BIG_SPENDER_5", Name = "Snack Attack!", Description = "Spent €5 or more", Category = AchievementCategory.SinglePurchase },
            new Achievement { Id = 2, Code = "BIG_SPENDER_10", Name = "Hunger Games Champion", Description = "Spent €10 or more", Category = AchievementCategory.SinglePurchase },
            new Achievement { Id = 3, Code = "DAILY_BUYER_5", Name = "Frequent Flyer", Description = "5+ purchases in a day", Category = AchievementCategory.DailyActivity },
            new Achievement { Id = 4, Code = "TOTAL_SPENT_100", Name = "Century Club", Description = "Spent €100+ total", Category = AchievementCategory.TotalSpent },
            new Achievement { Id = 5, Code = "IN_DEBT_50", Name = "Credit Card Lifestyle", Description = "€50+ unpaid", Category = AchievementCategory.HighDebt }
        );

        _context.SaveChanges();
    }

    [Fact]
    public async Task CheckAndAwardAchievements_SinglePurchase5Euros_AwardsBigSpender5()
    {
        // Arrange
        var purchase = new Purchase
        {
            Id = 1,
            UserId = 1,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            Scans = new List<BarcodeScan>
            {
                new BarcodeScan { Id = 1, PurchaseId = 1, BarcodeId = 1, Amount = 5.00m, ScannedAt = DateTime.UtcNow }
            }
        };
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert
        Assert.Single(achievements);
        Assert.Equal("BIG_SPENDER_5", achievements[0].Code);

        // Verify it was saved to database
        var userAchievement = await _context.UserAchievements.FirstOrDefaultAsync(ua => ua.UserId == 1);
        Assert.NotNull(userAchievement);
        Assert.Equal(1, userAchievement.AchievementId);
    }

    [Fact]
    public async Task CheckAndAwardAchievements_SinglePurchase10Euros_AwardsBigSpender10()
    {
        // Arrange
        var purchase = new Purchase
        {
            Id = 1,
            UserId = 1,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            Scans = new List<BarcodeScan>
            {
                new BarcodeScan { Id = 1, PurchaseId = 1, BarcodeId = 1, Amount = 10.00m, ScannedAt = DateTime.UtcNow }
            }
        };
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert
        Assert.Single(achievements);
        Assert.Equal("BIG_SPENDER_10", achievements[0].Code);
    }

    [Fact]
    public async Task CheckAndAwardAchievements_AlreadyEarned_DoesNotAwardAgain()
    {
        // Arrange - User already has BIG_SPENDER_5
        var existingAchievement = new UserAchievement
        {
            Id = 1,
            UserId = 1,
            AchievementId = 1, // BIG_SPENDER_5
            EarnedAt = DateTime.UtcNow.AddDays(-1),
            HasBeenShown = true
        };
        _context.UserAchievements.Add(existingAchievement);

        var purchase = new Purchase
        {
            Id = 1,
            UserId = 1,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            Scans = new List<BarcodeScan>
            {
                new BarcodeScan { Id = 1, PurchaseId = 1, BarcodeId = 1, Amount = 5.00m, ScannedAt = DateTime.UtcNow }
            }
        };
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert
        Assert.Empty(achievements); // Should not award again
    }

    [Fact]
    public async Task CheckAndAwardAchievements_TotalSpent100_AwardsTotalSpent100()
    {
        // Arrange - Add previous purchases totaling €95
        var oldPurchase = new Purchase
        {
            Id = 1,
            UserId = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            CompletedAt = DateTime.UtcNow.AddDays(-5),
            Scans = new List<BarcodeScan>
            {
                new BarcodeScan { Id = 1, PurchaseId = 1, BarcodeId = 1, Amount = 95.00m, ScannedAt = DateTime.UtcNow.AddDays(-5) }
            }
        };
        _context.Purchases.Add(oldPurchase);

        // New purchase that pushes total over €100
        var newPurchase = new Purchase
        {
            Id = 2,
            UserId = 1,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            Scans = new List<BarcodeScan>
            {
                new BarcodeScan { Id = 2, PurchaseId = 2, BarcodeId = 1, Amount = 10.00m, ScannedAt = DateTime.UtcNow }
            }
        };
        _context.Purchases.Add(newPurchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 2);

        // Assert
        Assert.Contains(achievements, a => a.Code == "TOTAL_SPENT_100");
    }

    [Fact]
    public async Task CheckAndAwardAchievements_HighDebt50_AwardsInDebt50()
    {
        // Arrange - Add purchases totaling €55 with no payments
        var purchase = new Purchase
        {
            Id = 1,
            UserId = 1,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            Scans = new List<BarcodeScan>
            {
                new BarcodeScan { Id = 1, PurchaseId = 1, BarcodeId = 1, Amount = 55.00m, ScannedAt = DateTime.UtcNow }
            }
        };
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert
        Assert.Contains(achievements, a => a.Code == "IN_DEBT_50");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
