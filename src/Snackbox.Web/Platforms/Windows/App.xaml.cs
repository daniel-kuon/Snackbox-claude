// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

// ReSharper disable once CheckNamespace
namespace Snackbox.Web.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App
{
	/// <summary>
	/// Initializes the singleton application object.  This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
	{
		InitializeComponent();

		// Exceptions on the UI thread end up here rather than in AppDomain.UnhandledException
		UnhandledException += (_, e) => MauiProgram.ReportCrash(e.Exception, $"WinUI: {e.Message}");
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
