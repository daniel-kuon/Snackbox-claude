namespace Snackbox.Web.Services;

public interface IWindowService
{
    void BringToFront();
    void SetFullscreen(bool fullscreen);
    void SetWindow(Window window);
}
