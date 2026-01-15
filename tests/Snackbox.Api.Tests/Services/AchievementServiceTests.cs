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
            new Achievement { Id = 1, Code = "BIG_SPENDER_2", Name = "Snack Nibbler", Description = "Spent €2 or more", Category = AchievementCategory.SinglePurchase },
            new Achievement { Id = 2, Code = "BIG_SPENDER_3", Name = "Snack Attack!", Description = "Spent €3 or more", Category = AchievementCategory.SinglePurchase },
            new Achievement { Id = 3, Code = "BIG_SPENDER_4", Name = "Hungry Hippo", Description = "Spent €4 or more", Category = AchievementCategory.SinglePurchase },
            new Achievement { Id = 4, Code = "DAILY_BUYER_5", Name = "Frequent Flyer", Description = "5+ purchases in a day", Category = AchievementCategory.DailyActivity },
            new Achievement { Id = 5, Code = "TOTAL_SPENT_100", Name = "Century Club", Description = "Spent €100+ total", Category = AchievementCategory.TotalSpent },
            new Achievement { Id = 6, Code = "IN_DEBT_15", Name = "Tab Starter", Description = "€15+ unpaid", Category = AchievementCategory.HighDebt }
        );

        _context.SaveChanges();
    }

    [Fact]
    public async Task CheckAndAwardAchievements_SinglePurchase3Euros_AwardsTwoTiers()
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
                new BarcodeScan { Id = 1, PurchaseId = 1, BarcodeId = 1, Amount = 3.00m, ScannedAt = DateTime.UtcNow }
            }
        };
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert - should award BIG_SPENDER_2 and BIG_SPENDER_3
        Assert.Equal(2, achievements.Count);
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_2");
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_3");

        // Verify it was saved to database
        var userAchievements = await _context.UserAchievements.Where(ua => ua.UserId == 1).ToListAsync();
        Assert.Equal(2, userAchievements.Count);
    }

    [Fact]
    public async Task CheckAndAwardAchievements_SinglePurchase4Euros_AwardsThreeTiers()
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
                new BarcodeScan { Id = 1, PurchaseId = 1, BarcodeId = 1, Amount = 4.00m, ScannedAt = DateTime.UtcNow }
            }
        };
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert - should award BIG_SPENDER_2, BIG_SPENDER_3, and BIG_SPENDER_4
        Assert.Equal(3, achievements.Count);
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_2");
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_3");
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_4");
    }

    [Fact]
    public async Task CheckAndAwardAchievements_AlreadyEarned_DoesNotAwardAgain()
    {
        // Arrange - User already has BIG_SPENDER_2
        var existingAchievement = new UserAchievement
        {
            Id = 1,
            UserId = 1,
            AchievementId = 1, // BIG_SPENDER_2
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
                new BarcodeScan { Id = 1, PurchaseId = 1, BarcodeId = 1, Amount = 2.50m, ScannedAt = DateTime.UtcNow }
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
    public async Task CheckAndAwardAchievements_SinglePurchase6Euros_AwardsAllFiveTiers()
    {
        // Arrange - Add remaining single purchase achievements
        _context.Achievements.AddRange(
            new Achievement { Id = 10, Code = "BIG_SPENDER_5", Name = "Snack Hoarder", Description = "Spent €5 or more", Category = AchievementCategory.SinglePurchase },
            new Achievement { Id = 11, Code = "BIG_SPENDER_6", Name = "The Whale", Description = "Spent €6 or more", Category = AchievementCategory.SinglePurchase }
        );
        await _context.SaveChangesAsync();

        var purchase = new Purchase
        {
            Id = 1,
            UserId = 1,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            Scans = new List<BarcodeScan>
            {
                new BarcodeScan { Id = 1, PurchaseId = 1, BarcodeId = 1, Amount = 6.00m, ScannedAt = DateTime.UtcNow }
            }
        };
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert - should award all five achievements
        Assert.Equal(5, achievements.Count);
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_2");
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_3");
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_4");
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_5");
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_6");
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
    public async Task CheckAndAwardAchievements_HighDebt15_AwardsInDebt15()
    {
        // Arrange - Add purchases totaling €20 with no payments
        var purchase = new Purchase
        {
            Id = 1,
            UserId = 1,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            Scans = new List<BarcodeScan>
            {
                new BarcodeScan { Id = 1, PurchaseId = 1, BarcodeId = 1, Amount = 20.00m, ScannedAt = DateTime.UtcNow }
            }
        };
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert
        Assert.Contains(achievements, a => a.Code == "IN_DEBT_15");
    }

    [Fact]
    public async Task CheckImmediateAchievements_IncompletePurchase_AwardsAchievements()
    {
        // Arrange - Create a purchase that is NOT completed (CompletedAt is default)
        var purchase = new Purchase
        {
            Id = 1,
            UserId = 1,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = default, // Not completed!
            Scans = new List<BarcodeScan>
            {
                new BarcodeScan { Id = 1, PurchaseId = 1, BarcodeId = 1, Amount = 3.00m, ScannedAt = DateTime.UtcNow }
            }
        };
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckImmediateAchievementsAsync(1, 1);

        // Assert - should award BIG_SPENDER_2 and BIG_SPENDER_3 even though purchase is not completed
        Assert.Equal(2, achievements.Count);
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_2");
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_3");
    }

    [Fact]
    public async Task CheckImmediateAchievements_MultipleScans_AwardsBasedOnTotal()
    {
        // Arrange - Create a purchase with multiple scans
        var purchase = new Purchase
        {
            Id = 1,
            UserId = 1,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = default,
            Scans = new List<BarcodeScan>
            {
                new BarcodeScan { Id = 1, PurchaseId = 1, BarcodeId = 1, Amount = 2.00m, ScannedAt = DateTime.UtcNow },
                new BarcodeScan { Id = 2, PurchaseId = 1, BarcodeId = 1, Amount = 2.00m, ScannedAt = DateTime.UtcNow }
            }
        };
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckImmediateAchievementsAsync(1, 1);

        // Assert - total is 4, should award BIG_SPENDER_2, BIG_SPENDER_3, and BIG_SPENDER_4
        Assert.Equal(3, achievements.Count);
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_2");
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_3");
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_4");
    }

    [Fact]
    public async Task CheckImmediateAchievements_HighDebt_AwardsWithoutCompletedPurchase()
    {
        // Arrange - Add IN_DEBT_20 achievement
        _context.Achievements.Add(
            new Achievement { Id = 10, Code = "IN_DEBT_20", Name = "Credit Curious", Description = "€20+ unpaid", Category = AchievementCategory.HighDebt }
        );
        
        // Add purchases totaling €20 with no payments (incomplete)
        var purchase = new Purchase
        {
            Id = 1,
            UserId = 1,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = default,
            Scans = new List<BarcodeScan>
            {
                new BarcodeScan { Id = 1, PurchaseId = 1, BarcodeId = 1, Amount = 20.00m, ScannedAt = DateTime.UtcNow }
            }
        };
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckImmediateAchievementsAsync(1, 1);

        // Assert - should award debt achievements for €15 and €20, plus single purchase achievements
        Assert.Contains(achievements, a => a.Code == "IN_DEBT_15");
        Assert.Contains(achievements, a => a.Code == "IN_DEBT_20");
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_2");
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_3");
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_4");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
