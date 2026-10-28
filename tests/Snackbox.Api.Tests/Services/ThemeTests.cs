using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Controllers;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Snackbox.Api.Services;
using Xunit;

namespace Snackbox.Api.Tests.Services;

public class ThemeTests : IDisposable
{
    private readonly ApplicationDbContext _context = new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Theme Halloween(ThemeMode mode = ThemeMode.Dates) => new()
    {
        Id = 1, Key = ThemeKeys.Halloween, Name = "Halloween", Description = "Spooky", Mode = mode,
        StartMonth = 10, StartDay = 24, EndMonth = 11, EndDay = 1
    };

    [Theory]
    [InlineData(10, 23, false)]
    [InlineData(10, 24, true)]
    [InlineData(10, 31, true)]
    [InlineData(11, 1, true)]
    [InlineData(11, 2, false)]
    [InlineData(6, 1, false)]
    public void Dates_ThatYearsWindow_BothEndsIncluded(int month, int day, bool expected) =>
        Assert.Equal(expected, Halloween().IsActiveOn(new DateTime(2026, month, day, 23, 59, 0)));

    [Fact]
    public void Dates_OverNewYear_Wrap()
    {
        var winter = Halloween();
        (winter.StartMonth, winter.StartDay, winter.EndMonth, winter.EndDay) = (12, 20, 1, 6);

        Assert.True(winter.IsActiveOn(new DateTime(2026, 12, 31)));
        Assert.True(winter.IsActiveOn(new DateTime(2027, 1, 3)));
        Assert.False(winter.IsActiveOn(new DateTime(2027, 1, 7)));
        // the run that started last December
        Assert.Equal(new DateTime(2026, 12, 20), winter.PeriodStart(new DateTime(2027, 1, 3)));
    }

    [Fact]
    public void OffAndAlways_IgnoreTheDates()
    {
        Assert.False(Halloween(ThemeMode.Off).IsActiveOn(new DateTime(2026, 10, 31)));
        Assert.True(Halloween(ThemeMode.Always).IsActiveOn(new DateTime(2026, 6, 1)));
    }

    [Fact]
    public async Task ActiveTheme_FollowsTheLocalDate()
    {
        _context.Themes.Add(Halloween());
        await _context.SaveChangesAsync();

        Assert.Equal("halloween", await ActiveAsync(new DateTime(2026, 10, 30, 12, 0, 0)));
        Assert.Null(await ActiveAsync(new DateTime(2026, 12, 1, 12, 0, 0)));
    }

    [Fact]
    public async Task HalloweenAchievements_OnlyWhileTheThemeIsOn()
    {
        _context.Themes.Add(Halloween());
        _context.Users.Add(new User { Id = 1, Username = "tina", CreatedAt = DateTime.UtcNow });
        _context.Achievements.AddRange(
            new Achievement { Id = 51, Code = "HALLOWEEN_TREAT", Name = "Trick or Treat!", Description = "x", Category = AchievementCategory.Seasonal, ThemeKey = ThemeKeys.Halloween },
            new Achievement { Id = 52, Code = "HALLOWEEN_DAY", Name = "Happy Halloween!", Description = "x", Category = AchievementCategory.Seasonal, ThemeKey = ThemeKeys.Halloween },
            new Achievement { Id = 53, Code = "HALLOWEEN_WITCHING_HOUR", Name = "Witching Hour", Description = "x", Category = AchievementCategory.Seasonal, ThemeKey = ThemeKeys.Halloween },
            new Achievement { Id = 54, Code = "HALLOWEEN_MONSTER", Name = "Monster Muncher", Description = "x", Category = AchievementCategory.Seasonal, ThemeKey = ThemeKeys.Halloween });
        var now = DateTime.UtcNow;
        for (var i = 1; i <= 5; i++)
        {
            _context.Purchases.Add(new Purchase
            {
                Id = i, UserId = 1, CreatedAt = now.AddMinutes(-i), UpdatedAt = now.AddMinutes(-i),
                Scans = new List<BarcodeScan> { new() { Id = i, PurchaseId = i, BarcodeId = 1, Amount = 0.30m, ScannedAt = now.AddMinutes(-i) } }
            });
        }
        await _context.SaveChangesAsync();

        // 31 October, 19:00 - all four
        var halloweenNight = await Earned(new DateTime(2026, 10, 31, 19, 0, 0));
        Assert.Equal(["HALLOWEEN_DAY", "HALLOWEEN_MONSTER", "HALLOWEEN_TREAT", "HALLOWEEN_WITCHING_HOUR"], halloweenNight);

        // December: none
        _context.UserAchievements.RemoveRange(_context.UserAchievements);
        await _context.SaveChangesAsync();
        Assert.Empty(await Earned(new DateTime(2026, 12, 3, 19, 0, 0)));
    }

    [Theory]
    [InlineData(2025, 11, 28)]
    [InlineData(2026, 11, 27)]
    [InlineData(2027, 11, 26)]
    [InlineData(2028, 11, 24)]
    public void BlackFriday_IsTheDayAfterTheFourthThursdayOfNovember(int year, int month, int day) =>
        Assert.Equal(new DateTime(year, month, day), ThemeCalendar.Day(ThemeKeys.BlackFriday, year));

    [Theory]
    [InlineData(2026, 7)] // a Sunday
    [InlineData(2025, 8)] // a Saturday
    [InlineData(2027, 6)] // a Monday
    [InlineData(2029, 6)] // a Thursday
    public void Nikolaus_MovesFromTheWeekendToMonday(int year, int day) =>
        Assert.Equal(new DateTime(year, 12, day), ThemeCalendar.Day(ThemeKeys.Nikolaus, year));

    [Fact]
    public async Task Overlapping_TheShorterRunShows()
    {
        _context.Themes.AddRange(
            new Theme { Id = 3, Key = ThemeKeys.Nikolaus, Name = "St. Nikolaus", Description = "x", Mode = ThemeMode.Auto },
            new Theme { Id = 4, Key = ThemeKeys.Christmas, Name = "Christmas", Description = "x", Mode = ThemeMode.Dates, StartMonth = 12, StartDay = 1, EndMonth = 12, EndDay = 26 });
        await _context.SaveChangesAsync();

        Assert.Equal(ThemeKeys.Christmas, await ActiveAsync(new DateTime(2026, 12, 6, 12, 0, 0)));
        Assert.Equal(ThemeKeys.Nikolaus, await ActiveAsync(new DateTime(2026, 12, 7, 12, 0, 0)));
        Assert.Equal(ThemeKeys.Christmas, await ActiveAsync(new DateTime(2026, 12, 8, 12, 0, 0)));
    }

    [Fact]
    public async Task ThemeDiscount_OnlyTheFirstPurchaseOfTheDay_AndOnlyOnThatDay()
    {
        _context.Themes.Add(new Theme { Id = 2, Key = ThemeKeys.BlackFriday, Name = "Black Friday", Description = "x", Mode = ThemeMode.Auto });
        _context.Discounts.Add(new Discount
        {
            Id = 9001, Name = "Black Friday", ThemeKey = ThemeKeys.BlackFriday, FirstPurchaseOnly = true, Type = DiscountType.Percentage, Value = 30,
            ValidFrom = new DateTime(2026, 1, 1), ValidTo = new DateTime(2100, 1, 1)
        });
        _context.Users.AddRange(new User { Id = 1, Username = "tina", CreatedAt = DateTime.UtcNow }, new User { Id = 2, Username = "tom", CreatedAt = DateTime.UtcNow });
        // Tina bought something the day before - that does not count
        AddPurchase(10, 1, new DateTime(2026, 11, 26, 15, 0, 0));
        AddPurchase(11, 1, new DateTime(2026, 11, 27, 9, 0, 0));
        AddPurchase(12, 1, new DateTime(2026, 11, 27, 11, 0, 0));
        AddPurchase(13, 2, new DateTime(2026, 11, 27, 11, 30, 0));
        AddPurchase(14, 2, new DateTime(2026, 11, 28, 9, 0, 0));
        await _context.SaveChangesAsync();

        Assert.Equal(0.84m, await DiscountedAsync(11, 1.20m, new DateTime(2026, 11, 27, 9, 0, 0)));   // Tina's first
        Assert.Equal(1.20m, await DiscountedAsync(12, 1.20m, new DateTime(2026, 11, 27, 11, 0, 0)));  // her second
        Assert.Equal(0.56m, await DiscountedAsync(13, 0.80m, new DateTime(2026, 11, 27, 11, 30, 0))); // Tom's first
        Assert.Equal(0.80m, await DiscountedAsync(14, 0.80m, new DateTime(2026, 11, 28, 9, 0, 0)));   // the day after
        // Tina's first, scanned again later that day, keeps it
        Assert.Equal(0.63m, await DiscountedAsync(11, 0.90m, new DateTime(2026, 11, 27, 16, 0, 0)));
    }

    private void AddPurchase(int id, int userId, DateTime at) =>
        _context.Purchases.Add(new Purchase { Id = id, UserId = userId, CreatedAt = at, UpdatedAt = at });

    private async Task<decimal> DiscountedAsync(int purchaseId, decimal total, DateTime localNow) =>
        (await PurchaseDiscountCalculator.ApplyAsync(_context, purchaseId, total, new FixedTime(localNow))).DiscountedAmount;

    private async Task<string?> ActiveAsync(DateTime localNow)
    {
        var result = await new ThemesController(_context, new FixedTime(localNow)).GetActive();
        return Assert.IsType<ActiveThemeDto>(Assert.IsType<OkObjectResult>(result.Result).Value).Key;
    }

    private async Task<List<string>> Earned(DateTime localNow)
    {
        var service = new AchievementService(_context, new FixedTime(localNow));
        return (await service.CheckImmediateAchievementsAsync(1, 1)).Select(a => a.Code).Where(c => c.StartsWith("HALLOWEEN")).Order().ToList();
    }

    /// <summary>A clock standing still at a local time, in UTC as the local zone.</summary>
    private sealed class FixedTime(DateTime localNow) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(localNow, DateTimeKind.Utc));
    }

    public void Dispose() => _context.Dispose();
}
