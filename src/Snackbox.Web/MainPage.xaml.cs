using Microsoft.AspNetCore.Components.WebView;

namespace Snackbox.Web;

public partial class MainPage : ContentPage
{
	private bool _isFirstLoad = true;

	public MainPage()
	{
		InitializeComponent();
	}

	private void OnUrlLoading(object? sender, UrlLoadingEventArgs e)
	{
		// On first load, navigate to /scan for MAUI app
		if (_isFirstLoad && e.Url.AbsolutePath == "/")
		{
			_isFirstLoad = false;
			e.UrlLoadingStrategy = UrlLoadingStrategy.OpenInWebView;

			// Navigate to scan page after a short delay to ensure Blazor is initialized
			Dispatcher.Dispatch(async () =>
			{
				await Task.Delay(100);
				await blazorWebView.TryDispatchAsync(sp =>
				{
					var navigationManager = sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
					navigationManager.NavigateTo("/scan", forceLoad: false);
				});
			});
		}
	}
}
