namespace Snackbox.Web.Services;

public class AppStateService
{
    private bool _showNavbar;

    public bool ShowNavbar
    {
        get => _showNavbar;
        private set
        {
            if (_showNavbar != value)
            {
                _showNavbar = value;
                OnChange?.Invoke();
            }
        }
    }

    public event Action? OnChange;

    public void SetNavbarVisible(bool visible)
    {
        ShowNavbar = visible;
    }

    public void HideNavbar() => SetNavbarVisible(false);

    public void ShowNavbarForAdmin() => SetNavbarVisible(true);
}
