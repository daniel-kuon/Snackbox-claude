using Snackbox.Api.Models;

namespace Snackbox.Api.Services;

/// <summary>
/// The ordered catalog of introduction wizard steps.
///
/// To add a step for a new feature: add an entry here (and matching content in the
/// SetupWizard component, keyed by the same string). Every user who has not recorded
/// that key yet will be shown it on their next scan - existing users included - without
/// replaying steps they already saw. Set <see cref="WizardStepDefinition.RequiredFeatureFlag"/>
/// to hide the step while that feature is switched off.
/// </summary>
public static class WizardStepCatalog
{
    public static readonly IReadOnlyList<WizardStepDefinition> Steps =
    [
        new("buying"),
        new("paying"),
        new("history"),
        new("mobile_app", RequiredFeatureFlag: FeatureFlagKeys.MobileApp)
    ];

    /// <summary>
    /// Steps the user still needs to see: never-seen steps, minus steps whose feature is off.
    /// </summary>
    public static List<string> GetPendingSteps(IEnumerable<string> seenStepKeys, ISet<string> enabledFeatureFlags)
    {
        var seen = seenStepKeys.ToHashSet();
        return Steps
            .Where(s => !seen.Contains(s.Key))
            .Where(s => s.RequiredFeatureFlag == null || enabledFeatureFlags.Contains(s.RequiredFeatureFlag))
            .Select(s => s.Key)
            .ToList();
    }
}

public record WizardStepDefinition(string Key, string? RequiredFeatureFlag = null);
