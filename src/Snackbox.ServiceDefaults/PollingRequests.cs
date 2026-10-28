namespace Snackbox.ServiceDefaults;

/// <summary>
/// Requests the apps repeat every few seconds just to stay current: health checks, feature
/// flags, the tablet's state. Traced and logged they were most of what SigNoz stored - and
/// ClickHouse's work on the kiosk PC - while saying nothing about any bug.
/// </summary>
public static class PollingRequests
{
    private static readonly string[] Paths = ["/health", "/alive", "/api/featureflags", "/api/tablet/state"];

    public static bool IsPolling(string? path) =>
        path != null && Paths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase)
                                       || path.StartsWith(p + "/", StringComparison.OrdinalIgnoreCase));
}
