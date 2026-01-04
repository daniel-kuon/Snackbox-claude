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

        // Get existing user achievements with the full achievement data
        var existingAchievementCodes = await _context.UserAchievements
            .Include(ua => ua.Achievement)
            .Where(ua => ua.UserId == userId)
            .Select(ua => ua.Achievement.Code)
            .ToListAsync();

        // Load all achievements once to avoid multiple database queries
        var allAchievements = await _context.Achievements.ToListAsync();
        var achievementLookup = allAchievements.ToDictionary(a => a.Code);

        // Calculate purchase amount
        var purchaseAmount = purchase.Scans.Sum(s => s.Amount);

        // Check single purchase achievements
        await CheckSinglePurchaseAchievements(userId, purchaseAmount, existingAchievementCodes, earnedAchievements, achievementLookup);

        // Check daily purchase count achievements
        await CheckDailyPurchaseAchievements(userId, purchase.CompletedAt.Value, existingAchievementCodes, earnedAchievements, achievementLookup);

        // Check streak achievements
        await CheckStreakAchievements(userId, purchase.CompletedAt.Value, existingAchievementCodes, earnedAchievements, achievementLookup);

        // Check comeback achievements
        await CheckComebackAchievements(userId, purchase.CompletedAt.Value, existingAchievementCodes, earnedAchievements, achievementLookup);

        // Check high debt achievements
        await CheckHighDebtAchievements(userId, existingAchievementCodes, earnedAchievements, achievementLookup);

        // Check total spent achievements
        await CheckTotalSpentAchievements(userId, existingAchievementCodes, earnedAchievements, achievementLookup);

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

    private Task CheckSinglePurchaseAchievements(int userId, decimal purchaseAmount, List<string> existingCodes, List<Achievement> earned, Dictionary<string, Achievement> achievementLookup)
    {
        if (purchaseAmount >= 15 && !existingCodes.Contains("BIG_SPENDER_15"))
        {
            if (achievementLookup.TryGetValue("BIG_SPENDER_15", out var achievement))
                earned.Add(achievement);
        }
        else if (purchaseAmount >= 10 && !existingCodes.Contains("BIG_SPENDER_10"))
        {
            if (achievementLookup.TryGetValue("BIG_SPENDER_10", out var achievement))
                earned.Add(achievement);
        }
        else if (purchaseAmount >= 5 && !existingCodes.Contains("BIG_SPENDER_5"))
        {
            if (achievementLookup.TryGetValue("BIG_SPENDER_5", out var achievement))
                earned.Add(achievement);
        }
        return Task.CompletedTask;
    }

    private async Task CheckDailyPurchaseAchievements(int userId, DateTime completedAt, List<string> existingCodes, List<Achievement> earned, Dictionary<string, Achievement> achievementLookup)
    {
        var today = completedAt.Date;
        var tomorrow = today.AddDays(1);

        var todayPurchaseCount = await _context.Purchases
            .Where(p => p.UserId == userId && p.CompletedAt >= today && p.CompletedAt < tomorrow)
            .CountAsync();

        if (todayPurchaseCount >= 5 && !existingCodes.Contains("DAILY_BUYER_5"))
        {
            if (achievementLookup.TryGetValue("DAILY_BUYER_5", out var achievement))
                earned.Add(achievement);
        }

        if (todayPurchaseCount >= 10 && !existingCodes.Contains("DAILY_BUYER_10"))
        {
            if (achievementLookup.TryGetValue("DAILY_BUYER_10", out var achievement))
                earned.Add(achievement);
        }
    }

    private async Task CheckStreakAchievements(int userId, DateTime completedAt, List<string> existingCodes, List<Achievement> earned, Dictionary<string, Achievement> achievementLookup)
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
            if (achievementLookup.TryGetValue("STREAK_DAILY_7", out var achievement))
                earned.Add(achievement);
        }
        else if (dailyStreak >= 3 && !existingCodes.Contains("STREAK_DAILY_3"))
        {
            if (achievementLookup.TryGetValue("STREAK_DAILY_3", out var achievement))
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
                if (achievementLookup.TryGetValue("STREAK_WEEKLY_4", out var achievement))
                    earned.Add(achievement);
            }
        }
    }

    private async Task CheckComebackAchievements(int userId, DateTime completedAt, List<string> existingCodes, List<Achievement> earned, Dictionary<string, Achievement> achievementLookup)
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
            if (achievementLookup.TryGetValue("COMEBACK_90", out var achievement))
                earned.Add(achievement);
        }
        else if (daysSinceLastPurchase >= 60 && !existingCodes.Contains("COMEBACK_60"))
        {
            if (achievementLookup.TryGetValue("COMEBACK_60", out var achievement))
                earned.Add(achievement);
        }
        else if (daysSinceLastPurchase >= 30 && !existingCodes.Contains("COMEBACK_30"))
        {
            if (achievementLookup.TryGetValue("COMEBACK_30", out var achievement))
                earned.Add(achievement);
        }
    }

    private async Task CheckHighDebtAchievements(int userId, List<string> existingCodes, List<Achievement> earned, Dictionary<string, Achievement> achievementLookup)
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
            if (achievementLookup.TryGetValue("IN_DEBT_150", out var achievement))
                earned.Add(achievement);
        }
        else if (debt >= 100 && !existingCodes.Contains("IN_DEBT_100"))
        {
            if (achievementLookup.TryGetValue("IN_DEBT_100", out var achievement))
                earned.Add(achievement);
        }
        else if (debt >= 50 && !existingCodes.Contains("IN_DEBT_50"))
        {
            if (achievementLookup.TryGetValue("IN_DEBT_50", out var achievement))
                earned.Add(achievement);
        }
    }

    private async Task CheckTotalSpentAchievements(int userId, List<string> existingCodes, List<Achievement> earned, Dictionary<string, Achievement> achievementLookup)
    {
        var totalSpent = await _context.BarcodeScans
            .Where(bs => bs.Purchase.UserId == userId)
            .SumAsync(bs => bs.Amount);

        if (totalSpent >= 200 && !existingCodes.Contains("TOTAL_SPENT_200"))
        {
            if (achievementLookup.TryGetValue("TOTAL_SPENT_200", out var achievement))
                earned.Add(achievement);
        }
        else if (totalSpent >= 150 && !existingCodes.Contains("TOTAL_SPENT_150"))
        {
            if (achievementLookup.TryGetValue("TOTAL_SPENT_150", out var achievement))
                earned.Add(achievement);
        }
        else if (totalSpent >= 100 && !existingCodes.Contains("TOTAL_SPENT_100"))
        {
            if (achievementLookup.TryGetValue("TOTAL_SPENT_100", out var achievement))
                earned.Add(achievement);
        }
    }
}
