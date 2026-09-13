using Snackbox.Api.Models;
using Snackbox.Api.Services;
using Xunit;

namespace Snackbox.Api.Tests.Services;

public class WizardStepCatalogTests
{
    private static readonly HashSet<string> NoFeatures = new();
    private static readonly HashSet<string> MobileOn = new() { FeatureFlagKeys.MobileApp };

    [Fact]
    public void NewUser_FeatureOff_GetsEveryUnflaggedStep()
    {
        var pending = WizardStepCatalog.GetPendingSteps([], NoFeatures);

        Assert.Equal(new[] { "welcome_new_version", "buying", "paying", "history" }, pending);
        Assert.DoesNotContain(FeatureFlagKeys.MobileApp, pending);
    }

    [Fact]
    public void NewUser_FeatureOn_AlsoGetsFlaggedStep()
    {
        var pending = WizardStepCatalog.GetPendingSteps([], MobileOn);

        Assert.Equal(new[] { "welcome_new_version", "buying", "paying", "history", FeatureFlagKeys.MobileApp }, pending);
    }

    [Fact]
    public void ExistingUser_FeatureEnabledLater_SeesOnlyTheNewStep()
    {
        // User already went through the intro while the feature was off
        string[] seen = ["welcome_new_version", "buying", "paying", "history"];

        var pending = WizardStepCatalog.GetPendingSteps(seen, MobileOn);

        var only = Assert.Single(pending);
        Assert.Equal(FeatureFlagKeys.MobileApp, only);
    }

    [Fact]
    public void UserWhoSawEverything_GetsNothing()
    {
        string[] seen = ["welcome_new_version", "buying", "paying", "history", FeatureFlagKeys.MobileApp];

        Assert.Empty(WizardStepCatalog.GetPendingSteps(seen, MobileOn));
    }

    [Fact]
    public void SeenFlaggedStep_StaysHidden_WhenFeatureTurnedBackOff()
    {
        string[] seen = ["welcome_new_version", "buying", "paying"];

        var pending = WizardStepCatalog.GetPendingSteps(seen, NoFeatures);

        Assert.Equal(new[] { "history" }, pending);
    }

    [Fact]
    public void PendingSteps_KeepCatalogOrder()
    {
        var pending = WizardStepCatalog.GetPendingSteps(["paying"], MobileOn);

        Assert.Equal(new[] { "welcome_new_version", "buying", "history", FeatureFlagKeys.MobileApp }, pending);
    }

    [Fact]
    public void EveryCatalogKeyIsUnique()
    {
        var keys = WizardStepCatalog.Steps.Select(s => s.Key).ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());
    }
}
