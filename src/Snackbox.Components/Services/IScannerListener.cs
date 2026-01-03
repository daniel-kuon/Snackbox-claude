namespace Snackbox.Web.Services;

public interface IScannerListener
{
    event Action<string>? CodeReceived;
    void Start();
    void Stop();
    void Dispose();
}