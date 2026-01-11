using Snackbox.Web.Services;

namespace Snackbox.Web;

public partial class App
{
	private readonly IWindowService _windowService;

	public App(IWindowService windowService)
	{
		InitializeComponent();
		_windowService = windowService;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new MainPage()) { Title = "Snackbox" };

		// Set initial window size
		window.Width = 1280;
		window.Height = 1024;

#if WINDOWS
		// Set window to fullscreen on Windows after window is created
		window.Created += (s, e) =>
		{
			_windowService.SetWindow(window);
		};
#endif

		return window;
	}
}
