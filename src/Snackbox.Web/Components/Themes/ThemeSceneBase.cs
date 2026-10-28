using Microsoft.AspNetCore.Components;

namespace Snackbox.Web.Components.Themes;

/// <summary>
/// What every theme scene gets from Home, and the welcome screen's play/rest cycle: any running
/// animation costs WebView2 a frame every vsync - about 8 % of a core even for one star - and the
/// welcome screen is up all day, so it plays in bursts and holds still in between. A scene pauses
/// its animations while <see cref="Resting"/> (animation-play-state: paused).
/// </summary>
public abstract class ThemeSceneBase : ComponentBase, IDisposable
{
    /// <summary>The welcome screen (true) or a purchase.</summary>
    [Parameter] public bool Welcome { get; set; }

    /// <summary>Scans of the current purchase - a scene can react to each new one.</summary>
    [Parameter] public int ScanCount { get; set; }

    /// <summary>Hold the foreground decoration back, e.g. while the intro wizard explains the screen.</summary>
    [Parameter] public bool Quiet { get; set; }

    private static readonly TimeSpan PlayFor = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan RestFor = TimeSpan.FromSeconds(135);
    private bool _resting;
    private bool _wasWelcome = true;
    private Timer? _cycle;

    /// <summary>The welcome screen is holding still right now.</summary>
    protected bool Resting => _resting && Welcome;

    protected override void OnInitialized() =>
        _cycle = new Timer(_ => InvokeAsync(() =>
        {
            _resting = !_resting;
            _cycle?.Change(_resting ? RestFor : PlayFor, Timeout.InfiniteTimeSpan);
            StateHasChanged();
        }), null, PlayFor, Timeout.InfiniteTimeSpan);

    protected override void OnParametersSet()
    {
        // Back on the welcome screen after a purchase: play a full burst first
        if (Welcome && !_wasWelcome)
        {
            _resting = false;
            _cycle?.Change(PlayFor, Timeout.InfiniteTimeSpan);
        }
        _wasWelcome = Welcome;
    }

    public void Dispose() => _cycle?.Dispose();

    // Styles are written with the invariant culture: the kiosk runs in German, where 12.5 is "12,5"
    protected static string Css(FormattableString style) => FormattableString.Invariant(style);
}
