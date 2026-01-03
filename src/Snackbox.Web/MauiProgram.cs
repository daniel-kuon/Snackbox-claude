using Microsoft.Extensions.Logging;
using Snackbox.Components.Services;
using Snackbox.Web.Services;

namespace Snackbox.Web;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>()
               .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddSingleton<WindowsScannerListener>()
               .AddSingleton<IScannerListener>(p => p.GetRequiredService<WindowsScannerListener>())
               .AddSingleton<AppStateService>();

        // Register storage service (MAUI secure storage)
        builder.Services.AddSingleton<IStorageService>(sp => new MauiStorageService(SecureStorage.Default));

        // Register delegating handler for authentication
        builder.Services.AddTransient<AuthenticationHeaderHandler>();

        // Register HttpClient for API calls
        string clientBaseAddress = builder.Configuration["API_HTTPS"] ??
                                   builder.Configuration["API_HTTP"] ??
                                   "http://localhost:5057" ??
                                   throw new InvalidOperationException("API URL is not configured.");

        // Add default HttpClient with BaseAddress for all other components (admin pages, etc.)
        builder.Services.AddHttpClient("DefaultClient", client => { client.BaseAddress = new Uri(clientBaseAddress); })
               .AddHttpMessageHandler<AuthenticationHeaderHandler>();

        // Also add a default unnamed HttpClient
        builder.Services.AddHttpClient("", client => { client.BaseAddress = new Uri(clientBaseAddress); })
               .AddHttpMessageHandler<AuthenticationHeaderHandler>();

        // For components that still use AddScoped<HttpClient>
        builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient(""));

        builder.Services.AddHttpClient<IAuthenticationService, AuthenticationService>(client =>
        {
            // Configure the base address for the API
            // This should be configurable based on environment
            client.BaseAddress = new Uri(clientBaseAddress);
        });

        builder.Services.AddHttpClient<IScannerService, ScannerService>(client =>
                                                                        {
                                                                            client.BaseAddress =
                                                                                new Uri(clientBaseAddress);
                                                                        })
               .AddHttpMessageHandler<AuthenticationHeaderHandler>();

        builder.Logging.AddDebug();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        return builder.Build();
    }
}
