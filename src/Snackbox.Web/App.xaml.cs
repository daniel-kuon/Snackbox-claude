namespace Snackbox.Web;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new MainPage()) { Title = "Snackbox.Web" };

		// Set initial window size
		window.Width = 1280;
		window.Height = 1024;

		return window;
	}
}
