namespace Snackbox.Web.Services;

public class DummyScannerListener : IScannerListener
{
#pragma warning disable CS0067 // Event is never used - this is a dummy implementation
    public event Action<string>? CodeReceived;
#pragma warning restore CS0067

    public void Start() { }

    public void Stop() { }

    public void Dispose() { }
}