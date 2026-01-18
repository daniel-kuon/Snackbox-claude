using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Models;

namespace Snackbox.Api.Services;

public interface IAchievementService
{
    Task<List<Achievement>> CheckAndAwardAchievementsAsync(int userId, int purchaseId);

    /// <summary>
    /// Check and award achievements that can be determined immediately during an active purchase.
    /// This includes single purchase amount achievements, high debt, and total spent achievements.
    /// </summary>
    Task<List<Achievement>> CheckImmediateAchievementsAsync(int userId, int purchaseId);
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

        if (purchase == null || purchase.UpdatedAt == default)
            return earnedAchievements;

        // Get full user achievement history for constraint checking
        var userAchievements = await _context.UserAchievements
            .Include(ua => ua.Achievement)
            .Where(ua => ua.UserId == userId)
            .OrderByDescending(ua => ua.EarnedAt)
            .ToListAsync();

        // Load all achievements once to avoid multiple database queries
        var allAchievements = await _context.Achievements.ToListAsync();
        var achievementLookup = allAchievements.ToDictionary(a => a.Code);

        // Calculate purchase amount
        var purchaseAmount = purchase.Scans.Sum(s => s.Amount);

        // Check single purchase achievements
        await CheckSinglePurchaseAchievements(userId, purchaseAmount, userAchievements, earnedAchievements, achievementLookup);

        // Check daily purchase count achievements
        await CheckDailyPurchaseAchievements(userId, purchase.UpdatedAt, userAchievements, earnedAchievements, achievementLookup);

        // Check streak achievements
        await CheckStreakAchievements(userId, purchase.UpdatedAt, userAchievements, earnedAchievements, achievementLookup);

        // Check comeback achievements
        await CheckComebackAchievements(userId, purchase.UpdatedAt, userAchievements, earnedAchievements, achievementLookup);

        // Check high debt achievements
        await CheckHighDebtAchievements(userId, userAchievements, earnedAchievements, achievementLookup);

        // Check total spent achievements
        await CheckTotalSpentAchievements(userId, userAchievements, earnedAchievements, achievementLookup);

        // Check time-based achievements
        await CheckTimeBasedAchievements(userId, purchase.UpdatedAt, userAchievements, earnedAchievements, achievementLookup);

        // Check milestone achievements
        await CheckMilestoneAchievements(userId, userAchievements, earnedAchievements, achievementLookup);

        // Check special achievements
        await CheckSpecialAchievements(userId, purchaseId, purchaseAmount, userAchievements, earnedAchievements, achievementLookup);

        // Save all earned achievements
        if (earnedAchievements.Any())
        {
            // Calculate current debt for debt-based achievements
            var totalSpent = await _context.BarcodeScans
                .Where(bs => bs.Purchase.UserId == userId)
                .SumAsync(bs => bs.Amount);
            var totalPaid = await _context.Payments
                .Where(p => p.UserId == userId)
                .SumAsync(p => p.Amount);
            var currentDebt = totalSpent - totalPaid;

            var newUserAchievements = earnedAchievements.Select(a => new UserAchievement
            {
                UserId = userId,
                AchievementId = a.Id,
                EarnedAt = DateTime.UtcNow,
                HasBeenShown = false,
                DebtAtEarning = a.Category == AchievementCategory.HighDebt ? currentDebt : null
            }).ToList();

            _context.UserAchievements.AddRange(newUserAchievements);
            await _context.SaveChangesAsync();
        }

        return earnedAchievements;
    }

    public async Task<List<Achievement>> CheckImmediateAchievementsAsync(int userId, int purchaseId)
    {
        var earnedAchievements = new List<Achievement>();

        // Get the current purchase (can be incomplete)
        var purchase = await _context.Purchases
            .Include(p => p.Scans)
            .FirstOrDefaultAsync(p => p.Id == purchaseId);

        if (purchase == null)
            return earnedAchievements;

        // Get full user achievement history for constraint checking
        var userAchievements = await _context.UserAchievements
            .Include(ua => ua.Achievement)
            .Where(ua => ua.UserId == userId)
            .OrderByDescending(ua => ua.EarnedAt)
            .ToListAsync();

        // Load all achievements once to avoid multiple database queries
        var allAchievements = await _context.Achievements.ToListAsync();
        var achievementLookup = allAchievements.ToDictionary(a => a.Code);

        // Calculate purchase amount
        var purchaseAmount = purchase.Scans.Sum(s => s.Amount);

        // Check single purchase achievements (based on current session total)
        await CheckSinglePurchaseAchievements(userId, purchaseAmount, userAchievements, earnedAchievements, achievementLookup);

        // Check high debt achievements (based on current balance)
        await CheckHighDebtAchievements(userId, userAchievements, earnedAchievements, achievementLookup);

        // Check total spent achievements (based on all-time spending)
        await CheckTotalSpentAchievements(userId, userAchievements, earnedAchievements, achievementLookup);

        // Check time-based achievements (current time)
        await CheckTimeBasedAchievements(userId, DateTime.UtcNow, userAchievements, earnedAchievements, achievementLookup);

        // Check milestone achievements (total purchase count)
        await CheckMilestoneAchievements(userId, userAchievements, earnedAchievements, achievementLookup);

        // Check special achievements
        await CheckSpecialAchievements(userId, purchaseId, purchaseAmount, userAchievements, earnedAchievements, achievementLookup);

        // Save all earned achievements
        if (earnedAchievements.Any())
        {
            // Calculate current debt for debt-based achievements
            var totalSpent = await _context.BarcodeScans
                .Where(bs => bs.Purchase.UserId == userId)
                .SumAsync(bs => bs.Amount);
            var totalPaid = await _context.Payments
                .Where(p => p.UserId == userId)
                .SumAsync(p => p.Amount);
            var currentDebt = totalSpent - totalPaid;

            var newUserAchievements = earnedAchievements.Select(a => new UserAchievement
            {
                UserId = userId,
                AchievementId = a.Id,
                EarnedAt = DateTime.UtcNow,
                HasBeenShown = false,
                DebtAtEarning = a.Category == AchievementCategory.HighDebt ? currentDebt : null
            }).ToList();

            _context.UserAchievements.AddRange(newUserAchievements);
            await _context.SaveChangesAsync();
        }

        return earnedAchievements;
    }

    private bool CanEarnAchievement(string achievementCode, AchievementCategory category, List<UserAchievement> userAchievements, decimal? currentDebt = null)
    {
        var previousAchievements = userAchievements.Where(ua => ua.Achievement.Code == achievementCode).ToList();

        // Never earned before - can earn
        if (!previousAchievements.Any())
            return true;

        var mostRecent = previousAchievements.First(); // Already sorted by EarnedAt descending
        var today = DateTime.UtcNow.Date;

        // No more than once per day (applies to all re-earnable achievements)
        if (mostRecent.EarnedAt.Date == today)
            return false;

        // Check category-specific rules
        switch (category)
        {
            case AchievementCategory.Milestone:
            case AchievementCategory.TotalSpent:
                // Can only be earned once ever
                return false;

            case AchievementCategory.HighDebt:
                // Can re-earn if debt dropped below the threshold since last earning
                if (!currentDebt.HasValue || !mostRecent.DebtAtEarning.HasValue)
                    return false;

                // Extract threshold from achievement code (e.g., "IN_DEBT_20" -> 20)
                var thresholdStr = achievementCode.Replace("IN_DEBT_", "");
                if (!decimal.TryParse(thresholdStr, out var threshold))
                    return false;

                // User can re-earn if their previous debt was >= threshold,
                // and current debt is >= threshold (they're back in debt territory)
                // Simplified: we assume if earning again, debt must have dropped in between
                return currentDebt.Value >= threshold;

            case AchievementCategory.TimeBased:
                // Monday/Friday can only be earned once per month
                if (achievementCode == "MONDAY_BLUES" || achievementCode == "FRIDAY_TREAT")
                {
                    return mostRecent.EarnedAt.Month != DateTime.UtcNow.Month ||
                           mostRecent.EarnedAt.Year != DateTime.UtcNow.Year;
                }
                // Other time-based can be earned daily (already checked above)
                return true;

            case AchievementCategory.Streak:
                // Can re-earn if previous streak was broken
                // This requires checking if there are any gaps in the purchase history
                // For now, allow re-earning after the streak is broken
                return true;

            case AchievementCategory.SinglePurchase:
            case AchievementCategory.DailyActivity:
            case AchievementCategory.Comeback:
            case AchievementCategory.Special:
                // Can re-earn once per day (already checked above)
                return true;

            default:
                return true;
        }
    }

    private Task CheckSinglePurchaseAchievements(int userId, decimal purchaseAmount, List<UserAchievement> userAchievements, List<Achievement> earned, Dictionary<string, Achievement> achievementLookup)
    {
        if (purchaseAmount >= 2 && achievementLookup.TryGetValue("BIG_SPENDER_2", out var achievement2))
        {
            if (CanEarnAchievement("BIG_SPENDER_2", AchievementCategory.SinglePurchase, userAchievements))
                earned.Add(achievement2);
        }

        if (purchaseAmount >= 3 && achievementLookup.TryGetValue("BIG_SPENDER_3", out var achievement3))
        {
            if (CanEarnAchievement("BIG_SPENDER_3", AchievementCategory.SinglePurchase, userAchievements))
                earned.Add(achievement3);
        }

        if (purchaseAmount >= 4 && achievementLookup.TryGetValue("BIG_SPENDER_4", out var achievement4))
        {
            if (CanEarnAchievement("BIG_SPENDER_4", AchievementCategory.SinglePurchase, userAchievements))
                earned.Add(achievement4);
        }

        if (purchaseAmount >= 5 && achievementLookup.TryGetValue("BIG_SPENDER_5", out var achievement5))
        {
            if (CanEarnAchievement("BIG_SPENDER_5", AchievementCategory.SinglePurchase, userAchievements))
                earned.Add(achievement5);
        }

        if (purchaseAmount >= 6 && achievementLookup.TryGetValue("BIG_SPENDER_6", out var achievement6))
        {
            if (CanEarnAchievement("BIG_SPENDER_6", AchievementCategory.SinglePurchase, userAchievements))
                earned.Add(achievement6);
        }

        return Task.CompletedTask;
    }

    private async Task CheckDailyPurchaseAchievements(int userId, DateTime completedAt, List<UserAchievement> userAchievements, List<Achievement> earned, Dictionary<string, Achievement> achievementLookup)
    {
        var today = completedAt.Date;
        var tomorrow = today.AddDays(1);

        var todayPurchaseCount = await _context.Purchases
            .Where(p => p.UserId == userId && p.UpdatedAt >= today && p.UpdatedAt < tomorrow)
            .CountAsync();

        if (todayPurchaseCount >= 3 && achievementLookup.TryGetValue("DAILY_BUYER_3", out var achievement3))
        {
            if (CanEarnAchievement("DAILY_BUYER_3", AchievementCategory.DailyActivity, userAchievements))
                earned.Add(achievement3);
        }

        if (todayPurchaseCount >= 5 && achievementLookup.TryGetValue("DAILY_BUYER_5", out var achievement5))
        {
            if (CanEarnAchievement("DAILY_BUYER_5", AchievementCategory.DailyActivity, userAchievements))
                earned.Add(achievement5);
        }

        if (todayPurchaseCount >= 10 && achievementLookup.TryGetValue("DAILY_BUYER_10", out var achievement10))
        {
            if (CanEarnAchievement("DAILY_BUYER_10", AchievementCategory.DailyActivity, userAchievements))
                earned.Add(achievement10);
        }
    }

    private async Task CheckStreakAchievements(int userId, DateTime completedAt, List<UserAchievement> userAchievements, List<Achievement> earned, Dictionary<string, Achievement> achievementLookup)
    {
        // Get all completed purchases, ordered by date
        var purchases = await _context.Purchases
            .Where(p => p.UserId == userId && p.UpdatedAt != default && p.UpdatedAt <= completedAt)
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => p.UpdatedAt.Date)
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

        if (dailyStreak >= 3 && achievementLookup.TryGetValue("STREAK_DAILY_3", out var achievement3))
        {
            if (CanEarnAchievement("STREAK_DAILY_3", AchievementCategory.Streak, userAchievements))
                earned.Add(achievement3);
        }

        if (dailyStreak >= 7 && achievementLookup.TryGetValue("STREAK_DAILY_7", out var achievement7))
        {
            if (CanEarnAchievement("STREAK_DAILY_7", AchievementCategory.Streak, userAchievements))
                earned.Add(achievement7);
        }

        if (dailyStreak >= 14 && achievementLookup.TryGetValue("STREAK_DAILY_14", out var achievement14))
        {
            if (CanEarnAchievement("STREAK_DAILY_14", AchievementCategory.Streak, userAchievements))
                earned.Add(achievement14);
        }

        if (dailyStreak >= 30 && achievementLookup.TryGetValue("STREAK_DAILY_30", out var achievement30))
        {
            if (CanEarnAchievement("STREAK_DAILY_30", AchievementCategory.Streak, userAchievements))
                earned.Add(achievement30);
        }

        // Check weekly streak (at least one purchase per week for 4 weeks)
        var fourWeeksAgo = completedAt.Date.AddDays(-28);
        var weeklyPurchases = purchases
            .Where(p => p >= fourWeeksAgo && p <= completedAt.Date)
            .ToList();

        bool hasWeeklyStreak = true;
        for (int week = 0; week < 4; week++)
        {
            var weekStart = fourWeeksAgo.AddDays((double)(week * 7));
            var weekEnd = weekStart.AddDays(7);
            if (!weeklyPurchases.Any(d => d >= weekStart && d < weekEnd))
            {
                hasWeeklyStreak = false;
                break;
            }
        }

        if (hasWeeklyStreak && achievementLookup.TryGetValue("STREAK_WEEKLY_4", out var achievementWeekly))
        {
            if (CanEarnAchievement("STREAK_WEEKLY_4", AchievementCategory.Streak, userAchievements))
                earned.Add(achievementWeekly);
        }
    }

    private async Task CheckComebackAchievements(int userId, DateTime completedAt, List<UserAchievement> userAchievements, List<Achievement> earned, Dictionary<string, Achievement> achievementLookup)
    {
        // Get the previous purchase before this one
        var previousPurchase = await _context.Purchases
            .Where(p => p.UserId == userId && p.UpdatedAt != default && p.UpdatedAt < completedAt)
            .OrderByDescending(p => p.UpdatedAt)
            .FirstOrDefaultAsync();

        if (previousPurchase == null || previousPurchase.UpdatedAt == default)
            return;

        var daysSinceLastPurchase = (completedAt - previousPurchase.UpdatedAt).TotalDays;

        if (daysSinceLastPurchase >= 30 && achievementLookup.TryGetValue("COMEBACK_30", out var achievement30))
        {
            if (CanEarnAchievement("COMEBACK_30", AchievementCategory.Comeback, userAchievements))
                earned.Add(achievement30);
        }

        if (daysSinceLastPurchase >= 60 && achievementLookup.TryGetValue("COMEBACK_60", out var achievement60))
        {
            if (CanEarnAchievement("COMEBACK_60", AchievementCategory.Comeback, userAchievements))
                earned.Add(achievement60);
        }

        if (daysSinceLastPurchase >= 90 && achievementLookup.TryGetValue("COMEBACK_90", out var achievement90))
        {
            if (CanEarnAchievement("COMEBACK_90", AchievementCategory.Comeback, userAchievements))
                earned.Add(achievement90);
        }
    }

    private async Task CheckHighDebtAchievements(int userId, List<UserAchievement> userAchievements, List<Achievement> earned, Dictionary<string, Achievement> achievementLookup)
    {
        var totalSpent = await _context.BarcodeScans
            .Where(bs => bs.Purchase.UserId == userId)
            .SumAsync(bs => bs.Amount);

        var totalPaid = await _context.Payments
            .Where(p => p.UserId == userId)
            .SumAsync(p => p.Amount);

        var debt = totalSpent - totalPaid;

        if (debt >= 15 && achievementLookup.TryGetValue("IN_DEBT_15", out var achievement15))
        {
            if (CanEarnAchievement("IN_DEBT_15", AchievementCategory.HighDebt, userAchievements, debt))
                earned.Add(achievement15);
        }

        if (debt >= 20 && achievementLookup.TryGetValue("IN_DEBT_20", out var achievement20))
        {
            if (CanEarnAchievement("IN_DEBT_20", AchievementCategory.HighDebt, userAchievements, debt))
                earned.Add(achievement20);
        }

        if (debt >= 25 && achievementLookup.TryGetValue("IN_DEBT_25", out var achievement25))
        {
            if (CanEarnAchievement("IN_DEBT_25", AchievementCategory.HighDebt, userAchievements, debt))
                earned.Add(achievement25);
        }

        if (debt >= 30 && achievementLookup.TryGetValue("IN_DEBT_30", out var achievement30))
        {
            if (CanEarnAchievement("IN_DEBT_30", AchievementCategory.HighDebt, userAchievements, debt))
                earned.Add(achievement30);
        }

        if (debt >= 35 && achievementLookup.TryGetValue("IN_DEBT_35", out var achievement35))
        {
            if (CanEarnAchievement("IN_DEBT_35", AchievementCategory.HighDebt, userAchievements, debt))
                earned.Add(achievement35);
        }
    }

    private async Task CheckTotalSpentAchievements(int userId, List<UserAchievement> userAchievements, List<Achievement> earned, Dictionary<string, Achievement> achievementLookup)
    {
        var totalSpent = await _context.BarcodeScans
            .Where(bs => bs.Purchase.UserId == userId)
            .SumAsync(bs => bs.Amount);

        // Total spent can only be earned once (checked by CanEarnAchievement)
        if (totalSpent >= 50 && achievementLookup.TryGetValue("TOTAL_SPENT_50", out var achievement50))
        {
            if (CanEarnAchievement("TOTAL_SPENT_50", AchievementCategory.TotalSpent, userAchievements))
                earned.Add(achievement50);
        }

        if (totalSpent >= 100 && achievementLookup.TryGetValue("TOTAL_SPENT_100", out var achievement100))
        {
            if (CanEarnAchievement("TOTAL_SPENT_100", AchievementCategory.TotalSpent, userAchievements))
                earned.Add(achievement100);
        }

        if (totalSpent >= 150 && achievementLookup.TryGetValue("TOTAL_SPENT_150", out var achievement150))
        {
            if (CanEarnAchievement("TOTAL_SPENT_150", AchievementCategory.TotalSpent, userAchievements))
                earned.Add(achievement150);
        }

        if (totalSpent >= 200 && achievementLookup.TryGetValue("TOTAL_SPENT_200", out var achievement200))
        {
            if (CanEarnAchievement("TOTAL_SPENT_200", AchievementCategory.TotalSpent, userAchievements))
                earned.Add(achievement200);
        }

        if (totalSpent >= 300 && achievementLookup.TryGetValue("TOTAL_SPENT_300", out var achievement300))
        {
            if (CanEarnAchievement("TOTAL_SPENT_300", AchievementCategory.TotalSpent, userAchievements))
                earned.Add(achievement300);
        }

        if (totalSpent >= 500 && achievementLookup.TryGetValue("TOTAL_SPENT_500", out var achievement500))
        {
            if (CanEarnAchievement("TOTAL_SPENT_500", AchievementCategory.TotalSpent, userAchievements))
                earned.Add(achievement500);
        }
    }

    private Task CheckTimeBasedAchievements(int userId, DateTime purchaseTime, List<UserAchievement> userAchievements, List<Achievement> earned, Dictionary<string, Achievement> achievementLookup)
    {
        var hour = purchaseTime.Hour;
        var dayOfWeek = purchaseTime.DayOfWeek;

        // Early Bird - before 8 AM
        if (hour < 8 && achievementLookup.TryGetValue("EARLY_BIRD", out var achievementEarly))
        {
            if (CanEarnAchievement("EARLY_BIRD", AchievementCategory.TimeBased, userAchievements))
                earned.Add(achievementEarly);
        }

        // Night Owl - after 8 PM
        if (hour >= 20 && achievementLookup.TryGetValue("NIGHT_OWL", out var achievementNight))
        {
            if (CanEarnAchievement("NIGHT_OWL", AchievementCategory.TimeBased, userAchievements))
                earned.Add(achievementNight);
        }

        // Lunch Rush - between 12-1 PM
        if (hour == 12 && achievementLookup.TryGetValue("LUNCH_RUSH", out var achievementLunch))
        {
            if (CanEarnAchievement("LUNCH_RUSH", AchievementCategory.TimeBased, userAchievements))
                earned.Add(achievementLunch);
        }

        // Weekend Warrior - Saturday or Sunday
        if (dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday && achievementLookup.TryGetValue("WEEKEND_WARRIOR", out var achievementWeekend))
        {
            if (CanEarnAchievement("WEEKEND_WARRIOR", AchievementCategory.TimeBased, userAchievements))
                earned.Add(achievementWeekend);
        }

        // Monday Blues (once per month)
        if (dayOfWeek == DayOfWeek.Monday && achievementLookup.TryGetValue("MONDAY_BLUES", out var achievementMonday))
        {
            if (CanEarnAchievement("MONDAY_BLUES", AchievementCategory.TimeBased, userAchievements))
                earned.Add(achievementMonday);
        }

        // Friday Treat (once per month)
        if (dayOfWeek == DayOfWeek.Friday && achievementLookup.TryGetValue("FRIDAY_TREAT", out var achievementFriday))
        {
            if (CanEarnAchievement("FRIDAY_TREAT", AchievementCategory.TimeBased, userAchievements))
                earned.Add(achievementFriday);
        }

        return Task.CompletedTask;
    }

    private async Task CheckMilestoneAchievements(int userId, List<UserAchievement> userAchievements, List<Achievement> earned, Dictionary<string, Achievement> achievementLookup)
    {
        var totalPurchases = await _context.Purchases
            .Where(p => p.UserId == userId && p.UpdatedAt != default)
            .CountAsync();

        // Also count incomplete purchases for first purchase achievement
        var allPurchases = await _context.Purchases
            .Where(p => p.UserId == userId)
            .CountAsync();

        // Milestones can only be earned once (checked by CanEarnAchievement)
        if (allPurchases >= 1 && achievementLookup.TryGetValue("FIRST_PURCHASE", out var achievementFirst))
        {
            if (CanEarnAchievement("FIRST_PURCHASE", AchievementCategory.Milestone, userAchievements))
                earned.Add(achievementFirst);
        }

        if (totalPurchases >= 10 && achievementLookup.TryGetValue("PURCHASE_10", out var achievement10))
        {
            if (CanEarnAchievement("PURCHASE_10", AchievementCategory.Milestone, userAchievements))
                earned.Add(achievement10);
        }

        if (totalPurchases >= 50 && achievementLookup.TryGetValue("PURCHASE_50", out var achievement50))
        {
            if (CanEarnAchievement("PURCHASE_50", AchievementCategory.Milestone, userAchievements))
                earned.Add(achievement50);
        }

        if (totalPurchases >= 100 && achievementLookup.TryGetValue("PURCHASE_100", out var achievement100))
        {
            if (CanEarnAchievement("PURCHASE_100", AchievementCategory.Milestone, userAchievements))
                earned.Add(achievement100);
        }

        if (totalPurchases >= 250 && achievementLookup.TryGetValue("PURCHASE_250", out var achievement250))
        {
            if (CanEarnAchievement("PURCHASE_250", AchievementCategory.Milestone, userAchievements))
                earned.Add(achievement250);
        }

        if (totalPurchases >= 500 && achievementLookup.TryGetValue("PURCHASE_500", out var achievement500))
        {
            if (CanEarnAchievement("PURCHASE_500", AchievementCategory.Milestone, userAchievements))
                earned.Add(achievement500);
        }
    }

    private async Task CheckSpecialAchievements(int userId, int purchaseId, decimal purchaseAmount, List<UserAchievement> userAchievements, List<Achievement> earned, Dictionary<string, Achievement> achievementLookup)
    {
        // Get current purchase with scans
        var purchase = await _context.Purchases
            .Include(p => p.Scans)
            .FirstOrDefaultAsync(p => p.Id == purchaseId);

        if (purchase == null)
            return;

        var scanCount = purchase.Scans.Count;

        // Double Trouble - exactly 2 scans in session
        if (scanCount == 2 && achievementLookup.TryGetValue("DOUBLE_TROUBLE", out var achievementDouble))
        {
            if (CanEarnAchievement("DOUBLE_TROUBLE", AchievementCategory.Special, userAchievements))
                earned.Add(achievementDouble);
        }

        // Triple Threat - exactly 3 scans in session
        if (scanCount == 3 && achievementLookup.TryGetValue("TRIPLE_THREAT", out var achievementTriple))
        {
            if (CanEarnAchievement("TRIPLE_THREAT", AchievementCategory.Special, userAchievements))
                earned.Add(achievementTriple);
        }

        // Lucky Seven - exactly 7 scans in session
        if (scanCount == 7 && achievementLookup.TryGetValue("LUCKY_SEVEN", out var achievementSeven))
        {
            if (CanEarnAchievement("LUCKY_SEVEN", AchievementCategory.Special, userAchievements))
                earned.Add(achievementSeven);
        }

        // // OCD Approved - round number totals (€5 or €10)
        // if ((purchaseAmount == 5m || purchaseAmount == 10m) && achievementLookup.TryGetValue("ROUND_NUMBER", out var achievementRound))
        // {
        //     if (CanEarnAchievement("ROUND_NUMBER", AchievementCategory.Special, userAchievements))
        //         earned.Add(achievementRound);
        // }

        // // Unlucky 13 - exactly €13
        // if (purchaseAmount == 13m && achievementLookup.TryGetValue("THIRTEENTH", out var achievementThirteen))
        // {
        //     if (CanEarnAchievement("THIRTEENTH", AchievementCategory.Special, userAchievements))
        //         earned.Add(achievementThirteen);
        // }
        //
        // // Nice - exactly €6.90
        // if (purchaseAmount == 6.90m && achievementLookup.TryGetValue("NICE", out var achievementNice))
        // {
        //     if (CanEarnAchievement("NICE", AchievementCategory.Special, userAchievements))
        //         earned.Add(achievementNice);
        // }

        // Speed Demon - 2 purchases within 1 minute
        if (scanCount >= 2 && achievementLookup.TryGetValue("SPEED_DEMON", out var achievementSpeed))
        {
            if (CanEarnAchievement("SPEED_DEMON", AchievementCategory.Special, userAchievements))
            {
                var scans = purchase.Scans.OrderBy(s => s.ScannedAt).ToList();
                for (int i = 1; i < scans.Count; i++)
                {
                    if ((scans[i].ScannedAt - scans[i - 1].ScannedAt).TotalSeconds <= 3)
                    {
                        earned.Add(achievementSpeed);
                        break;
                    }
                }
            }
        }

        // Debt Free - check if user has paid off their balance
        if (achievementLookup.TryGetValue("PAID_UP", out var achievementPaid))
        {
            if (CanEarnAchievement("PAID_UP", AchievementCategory.Special, userAchievements))
            {
                var totalSpent = await _context.BarcodeScans
                    .Where(bs => bs.Purchase.UserId == userId)
                    .SumAsync(bs => bs.Amount);

                var totalPaid = await _context.Payments
                    .Where(p => p.UserId == userId)
                    .SumAsync(p => p.Amount);

                // Must have spent something and be at zero or positive balance
                if (totalSpent > 0 && totalPaid >= totalSpent)
                {
                    earned.Add(achievementPaid);
                }
            }
        }

        // Generous Soul - positive balance of €10 or more
        if (achievementLookup.TryGetValue("GENEROUS_SOUL", out var achievementGenerous))
        {
            if (CanEarnAchievement("GENEROUS_SOUL", AchievementCategory.Special, userAchievements))
            {
                var totalSpent = await _context.BarcodeScans
                    .Where(bs => bs.Purchase.UserId == userId)
                    .SumAsync(bs => bs.Amount);

                var totalPaid = await _context.Payments
                    .Where(p => p.UserId == userId)
                    .SumAsync(p => p.Amount);

                if (totalPaid - totalSpent >= 10)
                {
                    earned.Add(achievementGenerous);
                }
            }
        }

        // Same Again - 3 identical purchases in a row (check scans in this purchase)
        if (scanCount >= 3 && achievementLookup.TryGetValue("SAME_AGAIN", out var achievementSame))
        {
            if (CanEarnAchievement("SAME_AGAIN", AchievementCategory.Special, userAchievements))
            {
                var scanAmounts = purchase.Scans.OrderBy(s => s.ScannedAt).Select(s => s.Amount).ToList();
                for (int i = 2; i < scanAmounts.Count; i++)
                {
                    if (scanAmounts[i] == scanAmounts[i - 1] && scanAmounts[i] == scanAmounts[i - 2])
                    {
                        earned.Add(achievementSame);
                        break;
                    }
                }
            }
        }

        // Snack Birthday - check if it's been exactly 1 year since first purchase
        if (achievementLookup.TryGetValue("SNACK_BIRTHDAY", out var achievementBirthday))
        {
            if (CanEarnAchievement("SNACK_BIRTHDAY", AchievementCategory.Special, userAchievements))
            {
                var firstPurchase = await _context.Purchases
                    .Where(p => p.UserId == userId)
                    .OrderBy(p => p.CreatedAt)
                    .FirstOrDefaultAsync();

                if (firstPurchase != null)
                {
                    var yearsSinceFirst = (DateTime.UtcNow - firstPurchase.CreatedAt).TotalDays;
                    // Check if it's between 365 and 367 days (giving a 2-day window)
                    if (yearsSinceFirst >= 365 && yearsSinceFirst <= 367)
                    {
                        earned.Add(achievementBirthday);
                    }
                }
            }
        }
    }
}
