using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Models;

namespace Snackbox.Api.Services;

public interface IAchievementService
{
    Task<List<Achievement>> CheckAndAwardAchievementsAsync(int userId, int purchaseId);
}

public class AchievementService : IAchievementService
{
    private readonly ApplicationDbContext _context;

    public AchievementService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Achievement>> CheckAndAwardAchievementsAsync(int userId, int purchaseId)
    {
        var earnedAchievements = new List<Achievement>();

        // Get the current purchase
        var purchase = await _context.Purchases
            .Include(p => p.Scans)
            .FirstOrDefaultAsync(p => p.Id == purchaseId);

        if (purchase == null || purchase.CompletedAt == null)
            return earnedAchievements;

        // Get existing user achievements
        var existingAchievementCodes = await _context.UserAchievements
            .Where(ua => ua.UserId == userId)
            .Select(ua => ua.Achievement.Code)
            .ToListAsync();

        // Calculate purchase amount
        var purchaseAmount = purchase.Scans.Sum(s => s.Amount);

        // Check single purchase achievements
        await CheckSinglePurchaseAchievements(userId, purchaseAmount, existingAchievementCodes, earnedAchievements);

        // Check daily purchase count achievements
        await CheckDailyPurchaseAchievements(userId, purchase.CompletedAt.Value, existingAchievementCodes, earnedAchievements);

        // Check streak achievements
        await CheckStreakAchievements(userId, purchase.CompletedAt.Value, existingAchievementCodes, earnedAchievements);

        // Check comeback achievements
        await CheckComebackAchievements(userId, purchase.CompletedAt.Value, existingAchievementCodes, earnedAchievements);

        // Check high debt achievements
        await CheckHighDebtAchievements(userId, existingAchievementCodes, earnedAchievements);

        // Check total spent achievements
        await CheckTotalSpentAchievements(userId, existingAchievementCodes, earnedAchievements);

        // Save all earned achievements
        if (earnedAchievements.Any())
        {
            var userAchievements = earnedAchievements.Select(a => new UserAchievement
            {
                UserId = userId,
                AchievementId = a.Id,
                EarnedAt = DateTime.UtcNow,
                HasBeenShown = false
            }).ToList();

            _context.UserAchievements.AddRange(userAchievements);
            await _context.SaveChangesAsync();
        }

        return earnedAchievements;
    }

    private async Task CheckSinglePurchaseAchievements(int userId, decimal purchaseAmount, List<string> existingCodes, List<Achievement> earned)
    {
        if (purchaseAmount >= 15 && !existingCodes.Contains("BIG_SPENDER_15"))
        {
            var achievement = await _context.Achievements.FirstAsync(a => a.Code == "BIG_SPENDER_15");
            earned.Add(achievement);
        }
        else if (purchaseAmount >= 10 && !existingCodes.Contains("BIG_SPENDER_10"))
        {
            var achievement = await _context.Achievements.FirstAsync(a => a.Code == "BIG_SPENDER_10");
            earned.Add(achievement);
        }
        else if (purchaseAmount >= 5 && !existingCodes.Contains("BIG_SPENDER_5"))
        {
            var achievement = await _context.Achievements.FirstAsync(a => a.Code == "BIG_SPENDER_5");
            earned.Add(achievement);
        }
    }

    private async Task CheckDailyPurchaseAchievements(int userId, DateTime completedAt, List<string> existingCodes, List<Achievement> earned)
    {
        var today = completedAt.Date;
        var tomorrow = today.AddDays(1);

        var todayPurchaseCount = await _context.Purchases
            .Where(p => p.UserId == userId && p.CompletedAt >= today && p.CompletedAt < tomorrow)
            .CountAsync();

        if (todayPurchaseCount >= 10 && !existingCodes.Contains("DAILY_BUYER_10"))
        {
            var achievement = await _context.Achievements.FirstAsync(a => a.Code == "DAILY_BUYER_10");
            earned.Add(achievement);
        }
        else if (todayPurchaseCount >= 5 && !existingCodes.Contains("DAILY_BUYER_5"))
        {
            var achievement = await _context.Achievements.FirstAsync(a => a.Code == "DAILY_BUYER_5");
            earned.Add(achievement);
        }
    }

    private async Task CheckStreakAchievements(int userId, DateTime completedAt, List<string> existingCodes, List<Achievement> earned)
    {
        // Get all completed purchases, ordered by date
        var purchases = await _context.Purchases
            .Where(p => p.UserId == userId && p.CompletedAt != null && p.CompletedAt <= completedAt)
            .OrderByDescending(p => p.CompletedAt)
            .Select(p => p.CompletedAt!.Value.Date)
            .Distinct()
            .ToListAsync();

        if (!purchases.Any())
            return;

        // Check daily streak
        int dailyStreak = 1;
        var currentDate = purchases[0];
        for (int i = 1; i < purchases.Count; i++)
        {
            if (currentDate.AddDays(-1) == purchases[i])
            {
                dailyStreak++;
                currentDate = purchases[i];
            }
            else
            {
                break;
            }
        }

        if (dailyStreak >= 7 && !existingCodes.Contains("STREAK_DAILY_7"))
        {
            var achievement = await _context.Achievements.FirstAsync(a => a.Code == "STREAK_DAILY_7");
            earned.Add(achievement);
        }
        else if (dailyStreak >= 3 && !existingCodes.Contains("STREAK_DAILY_3"))
        {
            var achievement = await _context.Achievements.FirstAsync(a => a.Code == "STREAK_DAILY_3");
            earned.Add(achievement);
        }

        // Check weekly streak (at least one purchase per week for 4 weeks)
        if (!existingCodes.Contains("STREAK_WEEKLY_4"))
        {
            var fourWeeksAgo = completedAt.Date.AddDays(-28);
            var weeklyPurchases = await _context.Purchases
                .Where(p => p.UserId == userId && p.CompletedAt >= fourWeeksAgo && p.CompletedAt <= completedAt)
                .Select(p => p.CompletedAt!.Value.Date)
                .ToListAsync();

            bool hasWeeklyStreak = true;
            for (int week = 0; week < 4; week++)
            {
                var weekStart = fourWeeksAgo.AddDays(week * 7);
                var weekEnd = weekStart.AddDays(7);
                if (!weeklyPurchases.Any(d => d >= weekStart && d < weekEnd))
                {
                    hasWeeklyStreak = false;
                    break;
                }
            }

            if (hasWeeklyStreak)
            {
                var achievement = await _context.Achievements.FirstAsync(a => a.Code == "STREAK_WEEKLY_4");
                earned.Add(achievement);
            }
        }
    }

    private async Task CheckComebackAchievements(int userId, DateTime completedAt, List<string> existingCodes, List<Achievement> earned)
    {
        // Get the previous purchase before this one
        var previousPurchase = await _context.Purchases
            .Where(p => p.UserId == userId && p.CompletedAt != null && p.CompletedAt < completedAt)
            .OrderByDescending(p => p.CompletedAt)
            .FirstOrDefaultAsync();

        if (previousPurchase?.CompletedAt == null)
            return;

        var daysSinceLastPurchase = (completedAt - previousPurchase.CompletedAt.Value).TotalDays;

        if (daysSinceLastPurchase >= 90 && !existingCodes.Contains("COMEBACK_90"))
        {
            var achievement = await _context.Achievements.FirstAsync(a => a.Code == "COMEBACK_90");
            earned.Add(achievement);
        }
        else if (daysSinceLastPurchase >= 60 && !existingCodes.Contains("COMEBACK_60"))
        {
            var achievement = await _context.Achievements.FirstAsync(a => a.Code == "COMEBACK_60");
            earned.Add(achievement);
        }
        else if (daysSinceLastPurchase >= 30 && !existingCodes.Contains("COMEBACK_30"))
        {
            var achievement = await _context.Achievements.FirstAsync(a => a.Code == "COMEBACK_30");
            earned.Add(achievement);
        }
    }

    private async Task CheckHighDebtAchievements(int userId, List<string> existingCodes, List<Achievement> earned)
    {
        var totalSpent = await _context.BarcodeScans
            .Where(bs => bs.Purchase.UserId == userId)
            .SumAsync(bs => bs.Amount);

        var totalPaid = await _context.Payments
            .Where(p => p.UserId == userId)
            .SumAsync(p => p.Amount);

        var debt = totalSpent - totalPaid;

        if (debt >= 150 && !existingCodes.Contains("IN_DEBT_150"))
        {
            var achievement = await _context.Achievements.FirstAsync(a => a.Code == "IN_DEBT_150");
            earned.Add(achievement);
        }
        else if (debt >= 100 && !existingCodes.Contains("IN_DEBT_100"))
        {
            var achievement = await _context.Achievements.FirstAsync(a => a.Code == "IN_DEBT_100");
            earned.Add(achievement);
        }
        else if (debt >= 50 && !existingCodes.Contains("IN_DEBT_50"))
        {
            var achievement = await _context.Achievements.FirstAsync(a => a.Code == "IN_DEBT_50");
            earned.Add(achievement);
        }
    }

    private async Task CheckTotalSpentAchievements(int userId, List<string> existingCodes, List<Achievement> earned)
    {
        var totalSpent = await _context.BarcodeScans
            .Where(bs => bs.Purchase.UserId == userId)
            .SumAsync(bs => bs.Amount);

        if (totalSpent >= 200 && !existingCodes.Contains("TOTAL_SPENT_200"))
        {
            var achievement = await _context.Achievements.FirstAsync(a => a.Code == "TOTAL_SPENT_200");
            earned.Add(achievement);
        }
        else if (totalSpent >= 150 && !existingCodes.Contains("TOTAL_SPENT_150"))
        {
            var achievement = await _context.Achievements.FirstAsync(a => a.Code == "TOTAL_SPENT_150");
            earned.Add(achievement);
        }
        else if (totalSpent >= 100 && !existingCodes.Contains("TOTAL_SPENT_100"))
        {
            var achievement = await _context.Achievements.FirstAsync(a => a.Code == "TOTAL_SPENT_100");
            earned.Add(achievement);
        }
    }
}
