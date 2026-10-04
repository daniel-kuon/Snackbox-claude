using Snackbox.Api.Services.LegacyImport;
using Xunit;

namespace Snackbox.Api.Tests.Services;

public class PurchaseGroupingTests
{
    private static readonly DateTime Start = new(2024, 5, 1, 12, 0, 0, DateTimeKind.Utc);

    private static List<List<DateTime>> Group(params int[] secondsFromStart) =>
        PurchaseGrouping.Group(secondsFromStart.Select(s => Start.AddSeconds(s)), t => t, 60);

    [Fact]
    public void Group_ScansInsideTheWindow_StayInOnePurchase()
    {
        var groups = Group(0, 10, 50);

        Assert.Single(groups);
        Assert.Equal(3, groups[0].Count);
    }

    [Fact]
    public void Group_GapAtLeastTheTimeout_StartsANewPurchase()
    {
        var groups = Group(0, 60);

        Assert.Equal(2, groups.Count);
    }

    [Fact]
    public void Group_MeasuresTheGapFromThePreviousScan_NotFromTheFirst()
    {
        // Each step is 40s, so the chain never breaks even though the last scan is 120s
        // after the first - the live scanner extends a running purchase the same way.
        var groups = Group(0, 40, 80, 120);

        Assert.Single(groups);
        Assert.Equal(4, groups[0].Count);
    }

    [Fact]
    public void Group_UnorderedInput_IsSortedFirst()
    {
        var groups = Group(200, 0, 10);

        Assert.Equal(2, groups.Count);
        Assert.Equal(2, groups[0].Count);
        Assert.Single(groups[1]);
        Assert.Equal(Start, groups[0][0]);
    }

    [Fact]
    public void Group_NoScans_ReturnsNothing()
    {
        Assert.Empty(Group());
    }
}
