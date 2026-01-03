using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebView;

namespace Snackbox.Web;

public partial class MainPage
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
			Dispatcher.DispatchAsync(async () =>
			{
				await Task.Delay(100);
				await BlazorWebView.TryDispatchAsync(sp =>
				{
					var navigationManager = sp.GetRequiredService<NavigationManager>();
					navigationManager.NavigateTo("/scan", forceLoad: false);
				});
			});
		}
	}
}
