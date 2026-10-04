namespace Snackbox.Api.Services.LegacyImport;

/// <summary>
/// The old Snackbox stored one row per barcode scan and had no notion of a purchase. The new
/// one groups consecutive scans of the same user into a purchase as long as they are less
/// than <c>Scanner:TimeoutSeconds</c> apart, so the import has to do the same thing to the
/// history - otherwise 27k single-item purchases show up where there were a few thousand.
/// </summary>
public static class PurchaseGrouping
{
    /// <summary>
    /// Splits one user's scans (any order) into groups. The gap is measured against the
    /// previous scan, matching how the live scanner extends a running purchase.
    /// </summary>
    public static List<List<T>> Group<T>(IEnumerable<T> scans, Func<T, DateTime> timeOf, int timeoutSeconds)
    {
        var groups = new List<List<T>>();
        var window = TimeSpan.FromSeconds(timeoutSeconds);
        DateTime? previous = null;

        foreach (var scan in scans.OrderBy(timeOf))
        {
            var time = timeOf(scan);
            if (previous == null || time - previous.Value >= window)
            {
                groups.Add(new List<T>());
            }

            groups[^1].Add(scan);
            previous = time;
        }

        return groups;
    }
}
