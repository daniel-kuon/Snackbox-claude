using Microsoft.Extensions.Logging;
using Snackbox.Components.Services;
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

		// Localization services (shared components use resource-based localization)
		builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
		builder.Services.AddSingleton<ILocalizationService, LocalizationService>();

		builder.Services.AddSingleton<WindowsScannerListener>();
		builder.Services.AddSingleton<AppStateService>();

		// Register storage service (MAUI secure storage)
		builder.Services.AddSingleton<IStorageService>(sp =>
			new MauiStorageService(SecureStorage.Default));

		// Register HttpClient for API calls
		string clientBaseAddress = builder.Configuration["API_HTTPS"] ?? builder.Configuration["API_HTTP"] ?? "http://localhost:5057"?? throw new InvalidOperationException("API URL is not configured.");
		builder.Services.AddHttpClient<IAuthenticationService, AuthenticationService>(client =>
		                                                                              {
			                                                                              // Configure the base address for the API
			                                                                              // This should be configurable based on environment
			                                                                              client.BaseAddress = new Uri(clientBaseAddress);
		                                                                              });

		builder.Services.AddHttpClient<IScannerService, ScannerService>(client =>
		                                                                {
			                                                                client.BaseAddress = new Uri(clientBaseAddress);
		                                                                });

		builder.Logging.AddDebug();

		// Set default culture to English
		System.Globalization.CultureInfo.DefaultThreadCurrentCulture = new("en");
		System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = new("en");

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
#endif

		return builder.Build();
	}
}
