using Snackbox.Components.Models;

namespace Snackbox.Components.Services;

/// <summary>
/// Filters a batch of freshly earned achievements for display: tiered achievements
/// (codes that differ only in a trailing number, e.g. COMEBACK_30/60/90) count as one
/// family, and only the highest tier earned in the batch is shown. Achievements without
/// a numeric suffix are their own family and always shown.
/// </summary>
public static class AchievementDisplayFilter
{
    public static List<Achievement> HighestPerFamily(IEnumerable<Achievement> achievements)
        => achievements
            .GroupBy(a => FamilyKey(a.Code))
            .Select(g => g.OrderByDescending(a => TierValue(a.Code)).First())
            .ToList();

    private static string FamilyKey(string code)
    {
        var i = code.LastIndexOf('_');
        return i > 0 && i < code.Length - 1 && code[(i + 1)..].All(char.IsDigit)
            ? code[..i]
            : code;
    }

    private static int TierValue(string code)
    {
        var i = code.LastIndexOf('_');
        return i > 0 && int.TryParse(code[(i + 1)..], out var tier) ? tier : 0;
    }
}
