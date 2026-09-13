using Snackbox.Components.Models;
using Snackbox.Components.Services;
using Xunit;

namespace Snackbox.Components.Tests.Services;

public class AchievementDisplayFilterTests
{
    private static Achievement Make(int id, string code) => new() { Id = id, Code = code, Name = code };

    [Fact]
    public void TieredFamily_OnlyHighestTierShown()
    {
        var earned = new List<Achievement>
        {
            Make(1, "COMEBACK_30"),
            Make(2, "COMEBACK_60"),
            Make(3, "COMEBACK_90")
        };

        var shown = AchievementDisplayFilter.HighestPerFamily(earned);

        var single = Assert.Single(shown);
        Assert.Equal("COMEBACK_90", single.Code);
    }

    [Fact]
    public void DifferentFamilies_AllShown()
    {
        var earned = new List<Achievement>
        {
            Make(1, "COMEBACK_90"),
            Make(2, "TOTAL_SPENT_100"),
            Make(3, "EARLY_BIRD")
        };

        var shown = AchievementDisplayFilter.HighestPerFamily(earned);

        Assert.Equal(3, shown.Count);
    }

    [Fact]
    public void NonNumericSuffixes_AreSeparateFamilies()
    {
        // EARLY_BIRD and NIGHT_OWL share no numeric tier - both must survive
        var earned = new List<Achievement>
        {
            Make(1, "EARLY_BIRD"),
            Make(2, "NIGHT_OWL"),
            Make(3, "STREAK_DAILY_3"),
            Make(4, "STREAK_DAILY_7"),
            Make(5, "STREAK_WEEKLY_4")
        };

        var shown = AchievementDisplayFilter.HighestPerFamily(earned);

        Assert.Equal(4, shown.Count);
        Assert.Contains(shown, a => a.Code == "STREAK_DAILY_7");
        Assert.DoesNotContain(shown, a => a.Code == "STREAK_DAILY_3");
        Assert.Contains(shown, a => a.Code == "STREAK_WEEKLY_4");
    }

    [Fact]
    public void MixedTiers_HighestNumberWinsNotAlphabetical()
    {
        // 100 > 50 numerically but "100" < "50" alphabetically
        var earned = new List<Achievement>
        {
            Make(1, "TOTAL_SPENT_100"),
            Make(2, "TOTAL_SPENT_50")
        };

        var shown = AchievementDisplayFilter.HighestPerFamily(earned);

        var single = Assert.Single(shown);
        Assert.Equal("TOTAL_SPENT_100", single.Code);
    }
}
