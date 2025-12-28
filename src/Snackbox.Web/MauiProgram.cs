using Microsoft.Extensions.Logging;
using Snackbox.Web.Services;

namespace Snackbox.Web;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

		// Register HttpClient for API calls
		builder.Services.AddHttpClient<IAuthenticationService, AuthenticationService>(client =>
		{
			// Configure the base address for the API
			// This should be configurable based on environment
			client.BaseAddress = new Uri("https://localhost:7000"); // Update with your API URL
		});

		// Register secure storage
		builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
