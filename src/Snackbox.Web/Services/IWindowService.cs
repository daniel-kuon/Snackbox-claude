namespace Snackbox.Web.Services;

public interface IWindowService
{
    /// <summary>Parallel-run mode: minimized, and scans never bring the window to the front.</summary>
    bool IsBackground { get; }
    void BringToFront();
    void SetFullscreen(bool fullscreen);
    void SetWindow(Window window);
}
