namespace Snackbox.Web.Services;

public class DummyScannerListener : IScannerListener
{
    public event Action<string>? CodeReceived;

    public void Start() { }

    public void Stop() { }

    public void Dispose() { }
}