namespace Snackbox.Api.Services;

/// <summary>
/// The one tablet session at a time: opened by a card scan of a user with the tablet feature,
/// ended by the Done button, by another user's scan or by the scanner timeout after the last
/// action. In memory - after an API restart the next scan simply opens a new one.
/// </summary>
public class TabletSessionStore(IConfiguration configuration)
{
    /// <param name="Picking">The user picks products here; otherwise the tablet only shows their card scans.</param>
    public record Session(int UserId, string Username, int PurchaseId, int BarcodeId, bool Picking, DateTime LastActivity);

    private readonly Lock _lock = new();
    private Session? _current;

    public TimeSpan Timeout => TimeSpan.FromSeconds(configuration.GetValue("Scanner:TimeoutSeconds", 60));

    public Session? Current
    {
        get
        {
            lock (_lock)
            {
                if (_current != null && _current.LastActivity + Timeout < DateTime.UtcNow) _current = null;
                return _current;
            }
        }
    }

    public void Start(Session session)
    {
        lock (_lock) _current = session;
    }

    /// <summary>Every pick restarts the timeout, like every scan does.</summary>
    public void Touch()
    {
        lock (_lock)
        {
            if (_current != null) _current = _current with { LastActivity = DateTime.UtcNow };
        }
    }

    public void End()
    {
        lock (_lock) _current = null;
    }

    /// <summary>Any scan by someone else closes the session - they are standing at the box now.</summary>
    public void EndUnlessUser(int userId)
    {
        lock (_lock)
        {
            if (_current != null && _current.UserId != userId) _current = null;
        }
    }
}
