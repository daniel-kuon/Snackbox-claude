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
            UpdatedAt = DateTime.UtcNow,
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
            UpdatedAt = DateTime.UtcNow,
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
        // Arrange - User already earned BIG_SPENDER_2 today
        // (re-earnable achievements can be earned at most once per day)
        var existingAchievement = new UserAchievement
        {
            Id = 1,
            UserId = 1,
            AchievementId = 1, // BIG_SPENDER_2
            EarnedAt = DateTime.UtcNow,
            HasBeenShown = true
        };
        _context.UserAchievements.Add(existingAchievement);

        var purchase = new Purchase
        {
            Id = 1,
            UserId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
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
            UpdatedAt = DateTime.UtcNow,
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
        // Arrange - Add previous purchases totaling €99.50, paid
        var oldPurchase = new Purchase
        {
            Id = 1,
            UserId = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-5),
            Scans = new List<BarcodeScan>
            {
                new BarcodeScan { Id = 1, PurchaseId = 1, BarcodeId = 1, Amount = 99.50m, ScannedAt = DateTime.UtcNow.AddDays(-5) }
            }
        };
        _context.Purchases.Add(oldPurchase);
        _context.Payments.Add(new Payment { Id = 1, UserId = 1, Amount = 100.50m, PaidAt = DateTime.UtcNow.AddDays(-5) });

        // New small purchase that pushes total over €100 (small: no big-purchase achievement
        // comes with it, which would hold total spent back)
        var newPurchase = new Purchase
        {
            Id = 2,
            UserId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Scans = new List<BarcodeScan>
            {
                new BarcodeScan { Id = 2, PurchaseId = 2, BarcodeId = 1, Amount = 1.00m, ScannedAt = DateTime.UtcNow }
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
            UpdatedAt = DateTime.UtcNow,
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
            UpdatedAt = default, // Not completed!
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
            UpdatedAt = default,
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
            UpdatedAt = default,
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

    [Fact]
    public async Task CheckAndAwardAchievements_LegacyImports_CountExceptForDailyActivity()
    {
        // Arrange - a migrated user with a pile of imported history and one real scan.
        _context.Achievements.AddRange(
            new Achievement { Id = 100, Code = "FIRST_PURCHASE", Name = "First Purchase", Description = "First one", Category = AchievementCategory.Milestone },
            new Achievement { Id = 101, Code = "PURCHASE_10", Name = "Regular", Description = "10 purchases", Category = AchievementCategory.Milestone },
            new Achievement { Id = 102, Code = "COMEBACK_30", Name = "Lazarus", Description = "Back after 30 days", Category = AchievementCategory.Comeback }
        );

        // Five of them landed "today" in the old system - those must not trigger DAILY_BUYER_5.
        for (var i = 0; i < 20; i++)
        {
            var when = i < 15 ? DateTime.UtcNow.AddDays(-400 + i) : DateTime.UtcNow.AddMinutes(-10);
            _context.Purchases.Add(new Purchase
            {
                Id = 100 + i,
                UserId = 1,
                IsLegacyImport = true,
                CreatedAt = when,
                UpdatedAt = when,
                Scans = new List<BarcodeScan>
                {
                    new BarcodeScan { Id = 100 + i, PurchaseId = 100 + i, BarcodeId = 1, Amount = 5.00m, ScannedAt = when }
                }
            });
        }

        _context.Purchases.Add(new Purchase
        {
            Id = 1,
            UserId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Scans = new List<BarcodeScan>
            {
                new BarcodeScan { Id = 1, PurchaseId = 1, BarcodeId = 1, Amount = 1.00m, ScannedAt = DateTime.UtcNow }
            }
        });
        _context.Payments.Add(new Payment { Id = 1, UserId = 1, Amount = 101.00m, PaidAt = DateTime.UtcNow.AddDays(-1) });
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAndAwardAchievementsAsync(1, 1);

        // Assert - history carries over for counts, spending and comebacks...
        Assert.Contains(achievements, a => a.Code == "FIRST_PURCHASE");
        Assert.Contains(achievements, a => a.Code == "PURCHASE_10");
        // ...but "how often did you buy today" only looks at purchases made here.
        Assert.DoesNotContain(achievements, a => a.Code == "DAILY_BUYER_5");
        // One lifetime category per purchase: total spent comes with the next one
        Assert.DoesNotContain(achievements, a => a.Code == "TOTAL_SPENT_100");
    }

    [Fact]
    public async Task LifetimeAchievements_OneCategoryPerQuietPurchase_PurchaseCountFirst()
    {
        // Arrange - a long-time user: €110 over 11 paid purchases, all of them in the past
        _context.Achievements.AddRange(
            new Achievement { Id = 100, Code = "FIRST_PURCHASE", Name = "First Purchase", Description = "First one", Category = AchievementCategory.Milestone },
            new Achievement { Id = 101, Code = "PURCHASE_10", Name = "Regular", Description = "10 purchases", Category = AchievementCategory.Milestone },
            new Achievement { Id = 102, Code = "TOTAL_SPENT_50", Name = "Fifty", Description = "€50", Category = AchievementCategory.TotalSpent });
        for (var i = 0; i < 11; i++)
        {
            var when = DateTime.UtcNow.AddDays(-20 + i);
            _context.Purchases.Add(new Purchase
            {
                Id = 100 + i, UserId = 1, CreatedAt = when, UpdatedAt = when,
                Scans = new List<BarcodeScan> { new() { Id = 100 + i, PurchaseId = 100 + i, BarcodeId = 1, Amount = 10.00m, ScannedAt = when } }
            });
        }
        _context.Payments.Add(new Payment { Id = 1, UserId = 1, Amount = 112.00m, PaidAt = DateTime.UtcNow.AddDays(-1) });
        await _context.SaveChangesAsync();

        // Act 1 - a purchase that brings a big-purchase achievement: nothing lifetime with it
        AddCurrentPurchase(1, 2.00m);
        var first = await _service.CheckImmediateAchievementsAsync(1, 1);
        Assert.Equal(["BIG_SPENDER_2"], first.Select(a => a.Code));

        // Act 2 - next scan of the same purchase: it already brought something
        Assert.Empty(await _service.CheckImmediateAchievementsAsync(1, 1));

        // Act 3 - a quiet purchase while the big-purchase one is still unshown: still waits
        AddCurrentPurchase(2, 0.50m);
        Assert.Empty(await _service.CheckImmediateAchievementsAsync(1, 2));

        // Act 4 - shown now, next quiet purchase: every purchase-count level, nothing else
        await MarkAllShownAsync();
        AddCurrentPurchase(3, 0.50m);
        var counts = await _service.CheckImmediateAchievementsAsync(1, 3);
        Assert.Equal(["FIRST_PURCHASE", "PURCHASE_10"], counts.Select(a => a.Code).Order());

        // Act 5 - the purchase after that: every total-spent level
        await MarkAllShownAsync();
        AddCurrentPurchase(4, 0.50m);
        var spent = await _service.CheckImmediateAchievementsAsync(1, 4);
        Assert.Equal(["TOTAL_SPENT_100", "TOTAL_SPENT_50"], spent.Select(a => a.Code).Order());

        // Act 6 - nothing left to hand out
        await MarkAllShownAsync();
        AddCurrentPurchase(5, 0.50m);
        Assert.Empty(await _service.CheckImmediateAchievementsAsync(1, 5));
    }

    [Fact]
    public async Task DebtAchievement_EarnedAgainOnlyAfterTheDebtWasPaidOff()
    {
        // Arrange - €20 in debt, IN_DEBT_15 earned two days ago
        var twoDaysAgo = DateTime.UtcNow.AddDays(-2);
        _context.Purchases.Add(new Purchase
        {
            Id = 100, UserId = 1, CreatedAt = twoDaysAgo, UpdatedAt = twoDaysAgo,
            Scans = new List<BarcodeScan> { new() { Id = 100, PurchaseId = 100, BarcodeId = 1, Amount = 19.50m, ScannedAt = twoDaysAgo } }
        });
        _context.UserAchievements.Add(new UserAchievement { UserId = 1, AchievementId = 6, EarnedAt = twoDaysAgo, HasBeenShown = true });
        await _context.SaveChangesAsync();

        // Still in debt on a later day: not again
        AddCurrentPurchase(1, 0.50m);
        Assert.DoesNotContain(await _service.CheckImmediateAchievementsAsync(1, 1), a => a.Code == "IN_DEBT_15");

        // Paid off yesterday, €20 in debt again now: earned again
        _context.Payments.Add(new Payment { Id = 1, UserId = 1, Amount = 20.00m, PaidAt = DateTime.UtcNow.AddDays(-1) });
        await _context.SaveChangesAsync();
        AddCurrentPurchase(2, 19.50m);
        Assert.Contains(await _service.CheckImmediateAchievementsAsync(1, 2), a => a.Code == "IN_DEBT_15");
    }

    [Fact]
    public async Task DebtFree_EarnedAgainOnlyAfterBeingBackInDebt()
    {
        // Arrange - €5 spent, €10 paid, Debt Free earned two days ago
        _context.Achievements.Add(new Achievement { Id = 46, Code = "PAID_UP", Name = "Debt Free!", Description = "Paid off", Category = AchievementCategory.Special });
        var twoDaysAgo = DateTime.UtcNow.AddDays(-2);
        _context.Purchases.Add(new Purchase
        {
            Id = 100, UserId = 1, CreatedAt = twoDaysAgo, UpdatedAt = twoDaysAgo,
            Scans = new List<BarcodeScan> { new() { Id = 100, PurchaseId = 100, BarcodeId = 1, Amount = 5.00m, ScannedAt = twoDaysAgo } }
        });
        _context.Payments.Add(new Payment { Id = 1, UserId = 1, Amount = 10.00m, PaidAt = twoDaysAgo });
        _context.UserAchievements.Add(new UserAchievement { UserId = 1, AchievementId = 46, EarnedAt = twoDaysAgo.AddMinutes(1), HasBeenShown = true });
        await _context.SaveChangesAsync();

        // Still paid up on a later day: not again
        AddCurrentPurchase(1, 0.50m);
        Assert.DoesNotContain(await _service.CheckImmediateAchievementsAsync(1, 1), a => a.Code == "PAID_UP");

        // Yesterday €10 in debt, then paid off: earned again
        var yesterday = DateTime.UtcNow.AddDays(-1);
        _context.Purchases.Add(new Purchase
        {
            Id = 101, UserId = 1, CreatedAt = yesterday, UpdatedAt = yesterday,
            Scans = new List<BarcodeScan> { new() { Id = 101, PurchaseId = 101, BarcodeId = 1, Amount = 10.00m, ScannedAt = yesterday } }
        });
        _context.Payments.Add(new Payment { Id = 2, UserId = 1, Amount = 10.00m, PaidAt = yesterday.AddHours(1) });
        await _context.SaveChangesAsync();
        AddCurrentPurchase(2, 0.50m);
        Assert.Contains(await _service.CheckImmediateAchievementsAsync(1, 2), a => a.Code == "PAID_UP");
    }

    private void AddCurrentPurchase(int id, decimal amount)
    {
        // A tick later than anything earned so far, as a real next purchase would be
        var now = DateTime.UtcNow.AddMilliseconds(id);
        _context.Purchases.Add(new Purchase
        {
            Id = id, UserId = 1, CreatedAt = now, UpdatedAt = now,
            Scans = new List<BarcodeScan> { new() { Id = id, PurchaseId = id, BarcodeId = 1, Amount = amount, ScannedAt = now } }
        });
        _context.SaveChanges();
    }

    private async Task MarkAllShownAsync()
    {
        foreach (var ua in _context.UserAchievements) ua.HasBeenShown = true;
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
