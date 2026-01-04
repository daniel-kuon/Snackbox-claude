namespace Snackbox.Components.Services;

public class SnackbarService
{
    public event Action<string, SnackbarType, int>? OnShow;

    public void Show(string message, SnackbarType type = SnackbarType.Info, int durationMs = 5000)
    {
        OnShow?.Invoke(message, type, durationMs);
    }

    public void Success(string message, int durationMs = 5000)
    {
        Show(message, SnackbarType.Success, durationMs);
    }

    public void Error(string message, int durationMs = 7000)
    {
        Show(message, SnackbarType.Error, durationMs);
    }

    public void Warning(string message, int durationMs = 6000)
    {
        Show(message, SnackbarType.Warning, durationMs);
    }

    public void Info(string message, int durationMs = 5000)
    {
        Show(message, SnackbarType.Info, durationMs);
    }
}

public enum SnackbarType
{
    Success,
    Error,
    Warning,
    Info
}
