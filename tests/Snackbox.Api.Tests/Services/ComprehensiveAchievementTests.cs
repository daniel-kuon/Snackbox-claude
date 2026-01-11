using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Models;
using Snackbox.Api.Services;
using Xunit;

namespace Snackbox.Api.Tests.Services;

/// <summary>
/// Comprehensive tests for all achievement types with positive and negative test cases.
/// </summary>
public class ComprehensiveAchievementTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AchievementService _service;

    public ComprehensiveAchievementTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _service = new AchievementService(_context);

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

        // Add all achievement types
        _context.Achievements.AddRange(
            // Single Purchase achievements
            new Achievement { Id = 1, Code = "BIG_SPENDER_2", Name = "Snack Nibbler", Description = "Spent €2 or more", Category = AchievementCategory.SinglePurchase },
            new Achievement { Id = 2, Code = "BIG_SPENDER_3", Name = "Snack Attack!", Description = "Spent €3 or more", Category = AchievementCategory.SinglePurchase },
            new Achievement { Id = 3, Code = "BIG_SPENDER_4", Name = "Hungry Hippo", Description = "Spent €4 or more", Category = AchievementCategory.SinglePurchase },
            new Achievement { Id = 4, Code = "BIG_SPENDER_5", Name = "Snack Hoarder", Description = "Spent €5 or more", Category = AchievementCategory.SinglePurchase },
            new Achievement { Id = 5, Code = "BIG_SPENDER_6", Name = "The Whale", Description = "Spent €6 or more", Category = AchievementCategory.SinglePurchase },
            
            // Daily Buyer achievements
            new Achievement { Id = 11, Code = "DAILY_BUYER_3", Name = "Snack Regular", Description = "3+ purchases in a day", Category = AchievementCategory.DailyActivity },
            new Achievement { Id = 12, Code = "DAILY_BUYER_5", Name = "Frequent Flyer", Description = "5+ purchases in a day", Category = AchievementCategory.DailyActivity },
            new Achievement { Id = 13, Code = "DAILY_BUYER_10", Name = "Snack Marathon", Description = "10+ purchases in a day", Category = AchievementCategory.DailyActivity },
            
            // Streak achievements
            new Achievement { Id = 21, Code = "STREAK_DAILY_3", Name = "Three-peat", Description = "3 days in a row", Category = AchievementCategory.Streak },
            new Achievement { Id = 22, Code = "STREAK_DAILY_7", Name = "Week Warrior", Description = "7 days in a row", Category = AchievementCategory.Streak },
            new Achievement { Id = 23, Code = "STREAK_DAILY_14", Name = "Fortnight Fighter", Description = "14 days in a row", Category = AchievementCategory.Streak },
            new Achievement { Id = 24, Code = "STREAK_DAILY_30", Name = "Monthly Master", Description = "30 days in a row", Category = AchievementCategory.Streak },
            new Achievement { Id = 25, Code = "STREAK_WEEKLY_4", Name = "Monthly Muncher", Description = "At least 1 purchase per week for 4 weeks", Category = AchievementCategory.Streak },
            
            // Comeback achievements
            new Achievement { Id = 31, Code = "COMEBACK_30", Name = "Long Time No See", Description = "First purchase after 30 days", Category = AchievementCategory.Comeback },
            new Achievement { Id = 32, Code = "COMEBACK_60", Name = "The Return", Description = "First purchase after 60 days", Category = AchievementCategory.Comeback },
            new Achievement { Id = 33, Code = "COMEBACK_90", Name = "Lazarus Rising", Description = "First purchase after 90 days", Category = AchievementCategory.Comeback },
            
            // High Debt achievements
            new Achievement { Id = 41, Code = "IN_DEBT_15", Name = "Tab Starter", Description = "€15+ unpaid", Category = AchievementCategory.HighDebt },
            new Achievement { Id = 42, Code = "IN_DEBT_20", Name = "Credit Curious", Description = "€20+ unpaid", Category = AchievementCategory.HighDebt },
            new Achievement { Id = 43, Code = "IN_DEBT_25", Name = "Deficit Dabbler", Description = "€25+ unpaid", Category = AchievementCategory.HighDebt },
            new Achievement { Id = 44, Code = "IN_DEBT_30", Name = "Balance Avoider", Description = "€30+ unpaid", Category = AchievementCategory.HighDebt },
            new Achievement { Id = 45, Code = "IN_DEBT_35", Name = "Living on the Edge", Description = "€35+ unpaid", Category = AchievementCategory.HighDebt },
            
            // Total Spent achievements
            new Achievement { Id = 51, Code = "TOTAL_SPENT_50", Name = "Half Century", Description = "Spent €50+ total", Category = AchievementCategory.TotalSpent },
            new Achievement { Id = 52, Code = "TOTAL_SPENT_100", Name = "Century Club", Description = "Spent €100+ total", Category = AchievementCategory.TotalSpent },
            new Achievement { Id = 53, Code = "TOTAL_SPENT_150", Name = "Snack Connoisseur", Description = "Spent €150+ total", Category = AchievementCategory.TotalSpent },
            new Achievement { Id = 54, Code = "TOTAL_SPENT_200", Name = "Snackbox Legend", Description = "Spent €200+ total", Category = AchievementCategory.TotalSpent },
            new Achievement { Id = 55, Code = "TOTAL_SPENT_300", Name = "Snack Tycoon", Description = "Spent €300+ total", Category = AchievementCategory.TotalSpent },
            new Achievement { Id = 56, Code = "TOTAL_SPENT_500", Name = "Snack Emperor", Description = "Spent €500+ total", Category = AchievementCategory.TotalSpent }
        );

        _context.SaveChanges();
    }

    #region Single Purchase Achievement Tests

    [Fact]
    public async Task SinglePurchase_ExactThreshold_AwardsAchievement()
    {
        // Arrange: Purchase exactly €2
        var purchase = CreatePurchase(1, 2.00m);
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert: Should award BIG_SPENDER_2
        Assert.Single(achievements);
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_2");
    }

    [Fact]
    public async Task SinglePurchase_BelowThreshold_DoesNotAwardAchievement()
    {
        // Arrange: Purchase €1.99 (below €2 threshold)
        var purchase = CreatePurchase(1, 1.99m);
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert: Should not award any achievements
        Assert.Empty(achievements);
    }

    [Fact]
    public async Task SinglePurchase_MultipleThresholds_AwardsAllQualified()
    {
        // Arrange: Purchase €5
        var purchase = CreatePurchase(1, 5.00m);
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert: Should award BIG_SPENDER_2, 3, 4, and 5
        Assert.Equal(4, achievements.Count);
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_2");
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_3");
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_4");
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_5");
    }

    [Fact]
    public async Task SinglePurchase_AlreadyEarned_DoesNotAwardAgain()
    {
        // Arrange: User already has BIG_SPENDER_2
        _context.UserAchievements.Add(new UserAchievement
        {
            UserId = 1,
            AchievementId = 1,
            EarnedAt = DateTime.UtcNow.AddDays(-1),
            HasBeenShown = true
        });
        await _context.SaveChangesAsync();

        var purchase = CreatePurchase(1, 2.00m);
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 2);

        // Assert: Should not award BIG_SPENDER_2 again
        Assert.DoesNotContain(achievements, a => a.Code == "BIG_SPENDER_2");
    }

    [Fact]
    public async Task SinglePurchase_MaxThreshold_AwardsAllTiers()
    {
        // Arrange: Purchase €6 (max threshold)
        var purchase = CreatePurchase(1, 6.00m);
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert: Should award all 5 tiers
        Assert.Equal(5, achievements.Count);
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_2");
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_3");
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_4");
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_5");
        Assert.Contains(achievements, a => a.Code == "BIG_SPENDER_6");
    }

    #endregion

    #region Daily Buyer Achievement Tests

    [Fact]
    public async Task DailyBuyer_ExactThreshold_AwardsAchievement()
    {
        // Arrange: Make exactly 3 purchases in one day
        var today = DateTime.UtcNow;
        for (int i = 1; i <= 3; i++)
        {
            var purchase = CreatePurchase(i, 1.00m, completedAt: today.AddMinutes(i * 10));
            _context.Purchases.Add(purchase);
        }
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 3);

        // Assert: Should award DAILY_BUYER_3
        Assert.Contains(achievements, a => a.Code == "DAILY_BUYER_3");
    }

    [Fact]
    public async Task DailyBuyer_BelowThreshold_DoesNotAwardAchievement()
    {
        // Arrange: Make only 2 purchases in one day
        var today = DateTime.UtcNow;
        for (int i = 1; i <= 2; i++)
        {
            var purchase = CreatePurchase(i, 1.00m, completedAt: today.AddMinutes(i * 10));
            _context.Purchases.Add(purchase);
        }
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 2);

        // Assert: Should not award any daily buyer achievements
        Assert.DoesNotContain(achievements, a => a.Code.StartsWith("DAILY_BUYER"));
    }

    [Fact]
    public async Task DailyBuyer_MultipleThresholds_AwardsAllQualified()
    {
        // Arrange: Make 10 purchases in one day
        var today = DateTime.UtcNow;
        for (int i = 1; i <= 10; i++)
        {
            var purchase = CreatePurchase(i, 1.00m, completedAt: today.AddMinutes(i * 5));
            _context.Purchases.Add(purchase);
        }
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 10);

        // Assert: Should award DAILY_BUYER_3, 5, and 10
        Assert.Contains(achievements, a => a.Code == "DAILY_BUYER_3");
        Assert.Contains(achievements, a => a.Code == "DAILY_BUYER_5");
        Assert.Contains(achievements, a => a.Code == "DAILY_BUYER_10");
    }

    [Fact]
    public async Task DailyBuyer_PurchasesOnDifferentDays_DoesNotCount()
    {
        // Arrange: Make 5 purchases across 2 days
        var today = DateTime.UtcNow;
        for (int i = 1; i <= 3; i++)
        {
            var purchase = CreatePurchase(i, 1.00m, completedAt: today.AddMinutes(i * 10));
            _context.Purchases.Add(purchase);
        }
        for (int i = 4; i <= 5; i++)
        {
            var purchase = CreatePurchase(i, 1.00m, completedAt: today.AddDays(1).AddMinutes(i * 10));
            _context.Purchases.Add(purchase);
        }
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 5);

        // Assert: Should not award DAILY_BUYER_5 (only 2 purchases today)
        Assert.DoesNotContain(achievements, a => a.Code == "DAILY_BUYER_5");
    }

    #endregion

    #region Streak Achievement Tests

    [Fact]
    public async Task DailyStreak_3Days_AwardsAchievement()
    {
        // Arrange: Make purchases for 3 consecutive days
        var today = DateTime.UtcNow;
        for (int day = 2; day >= 0; day--)
        {
            var purchase = CreatePurchase(3 - day, 1.00m, completedAt: today.AddDays(-day));
            _context.Purchases.Add(purchase);
        }
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 3);

        // Assert: Should award STREAK_DAILY_3
        Assert.Contains(achievements, a => a.Code == "STREAK_DAILY_3");
    }

    [Fact]
    public async Task DailyStreak_StreakBroken_DoesNotAwardAchievement()
    {
        // Arrange: Make purchases with a gap (day 0, day 2, day 3)
        var today = DateTime.UtcNow;
        var purchase1 = CreatePurchase(1, 1.00m, completedAt: today.AddDays(-3));
        var purchase2 = CreatePurchase(2, 1.00m, completedAt: today.AddDays(-1));
        var purchase3 = CreatePurchase(3, 1.00m, completedAt: today);
        _context.Purchases.AddRange(purchase1, purchase2, purchase3);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 3);

        // Assert: Should not award STREAK_DAILY_3 (streak broken)
        Assert.DoesNotContain(achievements, a => a.Code == "STREAK_DAILY_3");
    }

    [Fact]
    public async Task DailyStreak_7Days_AwardsMultipleAchievements()
    {
        // Arrange: Make purchases for 7 consecutive days
        var today = DateTime.UtcNow;
        for (int day = 6; day >= 0; day--)
        {
            var purchase = CreatePurchase(7 - day, 1.00m, completedAt: today.AddDays(-day));
            _context.Purchases.Add(purchase);
        }
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 7);

        // Assert: Should award STREAK_DAILY_3 and STREAK_DAILY_7
        Assert.Contains(achievements, a => a.Code == "STREAK_DAILY_3");
        Assert.Contains(achievements, a => a.Code == "STREAK_DAILY_7");
    }

    [Fact]
    public async Task WeeklyStreak_4Weeks_AwardsAchievement()
    {
        // Arrange: Make at least one purchase per week for 4 weeks
        var today = DateTime.UtcNow;
        for (int week = 0; week < 4; week++)
        {
            var purchase = CreatePurchase(week + 1, 1.00m, completedAt: today.AddDays(-week * 7));
            _context.Purchases.Add(purchase);
        }
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 4);

        // Assert: Should award STREAK_WEEKLY_4
        Assert.Contains(achievements, a => a.Code == "STREAK_WEEKLY_4");
    }

    [Fact]
    public async Task WeeklyStreak_MissingWeek_DoesNotAwardAchievement()
    {
        // Arrange: Make purchases in weeks 0, 1, 3 (missing week 2)
        var today = DateTime.UtcNow;
        var purchase1 = CreatePurchase(1, 1.00m, completedAt: today);
        var purchase2 = CreatePurchase(2, 1.00m, completedAt: today.AddDays(-7));
        var purchase3 = CreatePurchase(3, 1.00m, completedAt: today.AddDays(-21));
        _context.Purchases.AddRange(purchase1, purchase2, purchase3);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 3);

        // Assert: Should not award STREAK_WEEKLY_4 (week 2 missing)
        Assert.DoesNotContain(achievements, a => a.Code == "STREAK_WEEKLY_4");
    }

    #endregion

    #region Comeback Achievement Tests

    [Fact]
    public async Task Comeback_30Days_AwardsAchievement()
    {
        // Arrange: Last purchase was 30 days ago
        var lastPurchase = CreatePurchase(1, 1.00m, completedAt: DateTime.UtcNow.AddDays(-30));
        _context.Purchases.Add(lastPurchase);
        await _context.SaveChangesAsync();

        var newPurchase = CreatePurchase(2, 1.00m);
        _context.Purchases.Add(newPurchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 2);

        // Assert: Should award COMEBACK_30
        Assert.Contains(achievements, a => a.Code == "COMEBACK_30");
    }

    [Fact]
    public async Task Comeback_90Days_AwardsAllComebackAchievements()
    {
        // Arrange: Last purchase was 90 days ago
        var lastPurchase = CreatePurchase(1, 1.00m, completedAt: DateTime.UtcNow.AddDays(-90));
        _context.Purchases.Add(lastPurchase);
        await _context.SaveChangesAsync();

        var newPurchase = CreatePurchase(2, 1.00m);
        _context.Purchases.Add(newPurchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 2);

        // Assert: Should award all comeback achievements
        Assert.Contains(achievements, a => a.Code == "COMEBACK_30");
        Assert.Contains(achievements, a => a.Code == "COMEBACK_60");
        Assert.Contains(achievements, a => a.Code == "COMEBACK_90");
    }

    [Fact]
    public async Task Comeback_RecentPurchase_DoesNotAwardAchievement()
    {
        // Arrange: Last purchase was only 10 days ago
        var lastPurchase = CreatePurchase(1, 1.00m, completedAt: DateTime.UtcNow.AddDays(-10));
        _context.Purchases.Add(lastPurchase);
        await _context.SaveChangesAsync();

        var newPurchase = CreatePurchase(2, 1.00m);
        _context.Purchases.Add(newPurchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 2);

        // Assert: Should not award any comeback achievements
        Assert.DoesNotContain(achievements, a => a.Code.StartsWith("COMEBACK"));
    }

    [Fact]
    public async Task Comeback_FirstPurchaseEver_DoesNotAwardAchievement()
    {
        // Arrange: This is the user's first purchase
        var purchase = CreatePurchase(1, 1.00m);
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert: Should not award any comeback achievements
        Assert.DoesNotContain(achievements, a => a.Code.StartsWith("COMEBACK"));
    }

    #endregion

    #region High Debt Achievement Tests

    [Fact]
    public async Task HighDebt_ExactThreshold_AwardsAchievement()
    {
        // Arrange: Purchase exactly €15 with no payments
        var purchase = CreatePurchase(1, 15.00m);
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert: Should award IN_DEBT_15
        Assert.Contains(achievements, a => a.Code == "IN_DEBT_15");
    }

    [Fact]
    public async Task HighDebt_BelowThreshold_DoesNotAwardAchievement()
    {
        // Arrange: Purchase €14.99 with no payments
        var purchase = CreatePurchase(1, 14.99m);
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert: Should not award any debt achievements
        Assert.DoesNotContain(achievements, a => a.Code.StartsWith("IN_DEBT"));
    }

    [Fact]
    public async Task HighDebt_WithPayments_CalculatesNetDebt()
    {
        // Arrange: €20 spent, €10 paid = €10 net debt
        var purchase = CreatePurchase(1, 20.00m);
        _context.Purchases.Add(purchase);
        
        var payment = new Payment
        {
            UserId = 1,
            Amount = 10.00m,
            PaidAt = DateTime.UtcNow,
            Notes = "Test payment"
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert: Should not award any debt achievements (€10 < €15)
        Assert.DoesNotContain(achievements, a => a.Code.StartsWith("IN_DEBT"));
    }

    [Fact]
    public async Task HighDebt_MultipleThresholds_AwardsAllQualified()
    {
        // Arrange: Purchase €35 with no payments
        var purchase = CreatePurchase(1, 35.00m);
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert: Should award all debt achievements from 15 to 35
        Assert.Contains(achievements, a => a.Code == "IN_DEBT_15");
        Assert.Contains(achievements, a => a.Code == "IN_DEBT_20");
        Assert.Contains(achievements, a => a.Code == "IN_DEBT_25");
        Assert.Contains(achievements, a => a.Code == "IN_DEBT_30");
        Assert.Contains(achievements, a => a.Code == "IN_DEBT_35");
    }

    [Fact]
    public async Task HighDebt_NoDebt_DoesNotAwardAchievement()
    {
        // Arrange: €20 spent, €25 paid = -€5 (credit)
        var purchase = CreatePurchase(1, 20.00m);
        _context.Purchases.Add(purchase);
        
        var payment = new Payment
        {
            UserId = 1,
            Amount = 25.00m,
            PaidAt = DateTime.UtcNow,
            Notes = "Overpayment"
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert: Should not award any debt achievements (user has credit)
        Assert.DoesNotContain(achievements, a => a.Code.StartsWith("IN_DEBT"));
    }

    #endregion

    #region Total Spent Achievement Tests

    [Fact]
    public async Task TotalSpent_ExactThreshold_AwardsAchievement()
    {
        // Arrange: Spend exactly €50
        var purchase = CreatePurchase(1, 50.00m);
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert: Should award TOTAL_SPENT_50
        Assert.Contains(achievements, a => a.Code == "TOTAL_SPENT_50");
    }

    [Fact]
    public async Task TotalSpent_BelowThreshold_DoesNotAwardAchievement()
    {
        // Arrange: Spend €49.99
        var purchase = CreatePurchase(1, 49.99m);
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert: Should not award any total spent achievements
        Assert.DoesNotContain(achievements, a => a.Code.StartsWith("TOTAL_SPENT"));
    }

    [Fact]
    public async Task TotalSpent_AccumulatesAcrossPurchases_AwardsAchievement()
    {
        // Arrange: Two purchases totaling €100 (€60 + €40)
        var purchase1 = CreatePurchase(1, 60.00m, completedAt: DateTime.UtcNow.AddDays(-1));
        _context.Purchases.Add(purchase1);
        await _context.SaveChangesAsync();

        var purchase2 = CreatePurchase(2, 40.00m);
        _context.Purchases.Add(purchase2);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 2);

        // Assert: Should award TOTAL_SPENT_50 and TOTAL_SPENT_100
        Assert.Contains(achievements, a => a.Code == "TOTAL_SPENT_50");
        Assert.Contains(achievements, a => a.Code == "TOTAL_SPENT_100");
    }

    [Fact]
    public async Task TotalSpent_MultipleThresholds_AwardsAllQualified()
    {
        // Arrange: Single purchase of €500
        var purchase = CreatePurchase(1, 500.00m);
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert: Should award all total spent achievements
        Assert.Contains(achievements, a => a.Code == "TOTAL_SPENT_50");
        Assert.Contains(achievements, a => a.Code == "TOTAL_SPENT_100");
        Assert.Contains(achievements, a => a.Code == "TOTAL_SPENT_150");
        Assert.Contains(achievements, a => a.Code == "TOTAL_SPENT_200");
        Assert.Contains(achievements, a => a.Code == "TOTAL_SPENT_300");
        Assert.Contains(achievements, a => a.Code == "TOTAL_SPENT_500");
    }

    [Fact]
    public async Task TotalSpent_AlreadyEarned_DoesNotAwardAgain()
    {
        // Arrange: User already has TOTAL_SPENT_50, now spends another €50
        _context.UserAchievements.Add(new UserAchievement
        {
            UserId = 1,
            AchievementId = 51, // TOTAL_SPENT_50
            EarnedAt = DateTime.UtcNow.AddDays(-1),
            HasBeenShown = true
        });
        
        var oldPurchase = CreatePurchase(1, 50.00m, completedAt: DateTime.UtcNow.AddDays(-1));
        _context.Purchases.Add(oldPurchase);
        await _context.SaveChangesAsync();

        var newPurchase = CreatePurchase(2, 50.00m);
        _context.Purchases.Add(newPurchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 2);

        // Assert: Should award TOTAL_SPENT_100 but not TOTAL_SPENT_50 again
        Assert.Contains(achievements, a => a.Code == "TOTAL_SPENT_100");
        Assert.DoesNotContain(achievements, a => a.Code == "TOTAL_SPENT_50");
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public async Task MultipleCategories_SinglePurchase_AwardsMultipleTypes()
    {
        // Arrange: Large purchase that triggers multiple achievement types
        var purchase = CreatePurchase(1, 100.00m);
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert: Should award single purchase, high debt, and total spent achievements
        Assert.Contains(achievements, a => a.Code.StartsWith("BIG_SPENDER"));
        Assert.Contains(achievements, a => a.Code.StartsWith("IN_DEBT"));
        Assert.Contains(achievements, a => a.Code.StartsWith("TOTAL_SPENT"));
    }

    [Fact]
    public async Task NoAchievements_EmptyPurchase_ReturnsEmptyList()
    {
        // Arrange: Purchase with €0 amount
        var purchase = CreatePurchase(1, 0.00m);
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert: Should not award any achievements
        Assert.Empty(achievements);
    }

    [Fact]
    public async Task NonExistentPurchase_ReturnsEmptyList()
    {
        // Act: Check achievements for non-existent purchase
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 999);

        // Assert: Should return empty list
        Assert.Empty(achievements);
    }

    [Fact]
    public async Task IncompletePurchase_DoesNotCheckTimeBasedAchievements()
    {
        // Arrange: Purchase with CompletedAt = default (incomplete)
        var purchase = new Purchase
        {
            Id = 1,
            UserId = 1,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = default, // Not completed
            Scans = new List<BarcodeScan>
            {
                new BarcodeScan { Id = 1, PurchaseId = 1, BarcodeId = 1, Amount = 5.00m, ScannedAt = DateTime.UtcNow }
            }
        };
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert: Should not award any time-based achievements (streaks, daily, comeback)
        Assert.Empty(achievements);
        Assert.DoesNotContain(achievements, a => a.Code.StartsWith("STREAK"));
        Assert.DoesNotContain(achievements, a => a.Code.StartsWith("DAILY_BUYER"));
        Assert.DoesNotContain(achievements, a => a.Code.StartsWith("COMEBACK"));
    }

    #endregion

    #region Helper Methods

    private Purchase CreatePurchase(int id, decimal amount, DateTime? completedAt = null)
    {
        return new Purchase
        {
            Id = id,
            UserId = 1,
            CreatedAt = completedAt ?? DateTime.UtcNow,
            CompletedAt = completedAt ?? DateTime.UtcNow,
            Scans = new List<BarcodeScan>
            {
                new BarcodeScan 
                { 
                    Id = id, 
                    PurchaseId = id, 
                    BarcodeId = 1, 
                    Amount = amount, 
                    ScannedAt = completedAt ?? DateTime.UtcNow 
                }
            }
        };
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #endregion
}
