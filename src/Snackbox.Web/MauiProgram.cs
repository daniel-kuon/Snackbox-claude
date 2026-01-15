using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Snackbox.ApiClient;
using Snackbox.Components.Services;
using Snackbox.Web.Services;
using System.Reflection;

namespace Snackbox.Web;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>()
               .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

        // Load configuration from appsettings.json in Resources/Raw folder
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("Snackbox.Web.Resources.Raw.appsettings.json");
        if (stream != null)
        {
            var config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();
            builder.Configuration.AddConfiguration(config);
        }

        builder.Services.AddMauiBlazorWebView();

        // Register window service
        builder.Services.AddSingleton<IWindowService, WindowsWindowService>();

        builder.Services.AddSingleton<WindowsScannerListener>()
               .AddSingleton<IScannerListener>(p => p.GetRequiredService<WindowsScannerListener>());

        // Register storage service (MAUI secure storage)
        builder.Services.AddSingleton<IStorageService>(_ => new MauiStorageService(SecureStorage.Default));

        // Register Snackbar service
        builder.Services.AddScoped<SnackbarService>();

        // Register delegating handler for authentication
        builder.Services.AddTransient<AuthenticationHeaderHandler>();

        // Register HttpClient for API calls
        string clientBaseAddress = builder.Configuration["API_HTTPS"] ??
                                   builder.Configuration["API_HTTP"] ?? "http://localhost:5057";

        // Add default HttpClient with BaseAddress for all other components (admin pages, etc.)
        builder.Services.AddHttpClient("DefaultClient", client => { client.BaseAddress = new Uri(clientBaseAddress); })
               .AddHttpMessageHandler<AuthenticationHeaderHandler>();

        // Also add a default unnamed HttpClient
        builder.Services.AddHttpClient("", client => { client.BaseAddress = new Uri(clientBaseAddress); })
               .AddHttpMessageHandler<AuthenticationHeaderHandler>();

        // Register all Snackbox API clients with authentication
        builder.Services.AddSnackboxApiClientWithAuth<AuthenticationHeaderHandler>(clientBaseAddress);

        builder.Services.AddHttpClient<IAuthenticationService, AuthenticationService>(client =>
        {
            client.BaseAddress = new Uri(clientBaseAddress);
        });

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
