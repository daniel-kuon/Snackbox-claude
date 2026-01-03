namespace Snackbox.Components.Services;

public interface IScannerListener
{
    event Action<string>? CodeReceived;
    void Start();
}
