using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebView;

namespace Snackbox.Web;

public partial class MainPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private void OnUrlLoading(object? sender, UrlLoadingEventArgs e)
	{
		// Allow Blazor routing to handle navigation (Index.razor will check database and route appropriately)
		e.UrlLoadingStrategy = UrlLoadingStrategy.OpenInWebView;
	}
}
