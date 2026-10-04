namespace Snackbox.Web.Configuration;

public class WindowConfiguration
{
    public bool StartFullscreen { get; set; }
    public bool AutoFocusOnScan { get; set; }

    /// <summary>
    /// Parallel-run mode: the kiosk starts minimized and stays there, so the old Snackbox can
    /// own the screen. Scans are still recorded (the keyboard hook is global), the window just
    /// does not pop up. Overrides <see cref="StartFullscreen"/> and <see cref="AutoFocusOnScan"/>.
    /// </summary>
    public bool StartMinimized { get; set; }
}
