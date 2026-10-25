namespace Snackbox.Api.Services;

/// <summary>
/// The one tablet session at a time: opened by a card scan of a user with the tablet feature,
/// ended by another user's scan or when it runs out. In memory - after an API restart the next
/// scan simply opens a new one.
///
/// How long it lasts depends on what the tablet shows: the scanner timeout after the last action
/// while buying; 5 seconds after Done (the purchase is closed, a tap on a product starts a new
/// one); 2 minutes without a touch while the introduction or a dialog is open (paused countdown).
/// </summary>
public class TabletSessionStore(IConfiguration configuration)
{
    /// <param name="PurchaseId">Null after Done until the next pick starts a new purchase.</param>
    /// <param name="Picking">The user picks products here; otherwise the tablet only shows their card scans.</param>
    public record Session(int UserId, string Username, int? PurchaseId, int BarcodeId, bool Picking, DateTime LastActivity,
                          bool Completed = false, bool Paused = false);

    public static readonly TimeSpan AfterDone = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan PausedIdle = TimeSpan.FromMinutes(2);
    private const int ClosedPurchasesKept = 50;

    private readonly Lock _lock = new();
    private readonly LinkedList<int> _closedPurchases = new();
    private Session? _current;

    public TimeSpan Timeout => TimeSpan.FromSeconds(configuration.GetValue("Scanner:TimeoutSeconds", 60));

    public DateTime ExpiresAt(Session session) => session.LastActivity + (session.Completed ? AfterDone : session.Paused ? PausedIdle : Timeout);

    public Session? Current
    {
        get
        {
            lock (_lock)
            {
                if (_current != null && ExpiresAt(_current) < DateTime.UtcNow) _current = null;
                return _current;
            }
        }
    }

    public void Start(Session session)
    {
        lock (_lock) _current = session;
    }

    /// <summary>Every pick restarts the timeout, like every scan does.</summary>
    public void Touch() => Update(s => s with { LastActivity = DateTime.UtcNow });

    /// <summary>Introduction or a dialog open: the countdown stops, 2 minutes without a touch end it.</summary>
    public void SetPaused(bool paused) => Update(s => s with { Paused = paused, LastActivity = DateTime.UtcNow });

    /// <summary>Done: the purchase is over; the tablet stays on a few seconds for the next one.</summary>
    public void Complete(int? closedPurchaseId)
    {
        lock (_lock)
        {
            if (closedPurchaseId is { } id)
            {
                _closedPurchases.AddLast(id);
                if (_closedPurchases.Count > ClosedPurchasesKept) _closedPurchases.RemoveFirst();
            }
            if (_current != null) _current = _current with { PurchaseId = null, Completed = true, Paused = false, LastActivity = DateTime.UtcNow };
        }
    }

    /// <summary>A pick after Done: the session goes on with a new purchase.</summary>
    public void StartNewPurchase(int purchaseId) =>
        Update(s => s with { PurchaseId = purchaseId, Completed = false, LastActivity = DateTime.UtcNow });

    /// <summary>Ended with Done: the scanner must not add the next scan to it, however soon it comes.</summary>
    public bool IsClosed(int purchaseId)
    {
        lock (_lock) return _closedPurchases.Contains(purchaseId);
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

    private void Update(Func<Session, Session> change)
    {
        lock (_lock)
        {
            if (_current != null) _current = change(_current);
        }
    }
}
