using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Snackbox.ApiClient;
using Snackbox.Components.Services;
using Snackbox.Web.Services;
using Snackbox.ServiceDefaults;
using OpenTelemetry;
using OpenTelemetry.Trace; // TracerProvider.ForceFlush extension
using Snackbox.ServiceDefaults.Tracing;
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

        builder.Services.AddSnackboxOpenTelemetry(builder.Configuration, new TelemetryInstrumentationOptions
        {
            ServiceName = "snackbox-maui",
            EnableAspNetCoreInstrumentation = false,
            EnableHttpClientInstrumentation = true
        });
        builder.Logging.AddSnackboxOpenTelemetryLogging(builder.Configuration, "snackbox-maui");

        builder.Services.AddMauiBlazorWebView();

        // Register window service
        builder.Services.AddSingleton<IWindowService, WindowsWindowService>();

        builder.Services.AddSingleton<WindowsScannerListener>()
               .AddSingleton<IScannerListener>(p => p.GetRequiredService<WindowsScannerListener>());

        // Register storage service (MAUI secure storage)
        builder.Services.AddSingleton<IStorageService>(_ => new MauiStorageService(SecureStorage.Default));

        builder.Services.AddTransient<IUiTelemetry, UiTelemetry>();

        // Register Snackbar service
        builder.Services.AddScoped<SnackbarService>();
        builder.Services.AddSingleton<AppStartupState>();

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

        // Development-only UI test helper (API only answers in Development with TestHelper:Enabled)
        builder.Services.AddHttpClient<TestHelperClient>(client => { client.BaseAddress = new Uri(clientBaseAddress); });

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
                                                                            // Disable HTTP caching for scanner service
                                                                            client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
                                                                            {
                                                                                NoCache = true,
                                                                                NoStore = true,
                                                                                MustRevalidate = true
                                                                            };
                                                                            client.DefaultRequestHeaders.Pragma.Add(new System.Net.Http.Headers.NameValueHeaderValue("no-cache"));
                                                                        })
               .AddHttpMessageHandler<AuthenticationHeaderHandler>();

        builder.Logging.AddDebug();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        // [Traced] interception for the typed-HttpClient services (their implementation type
        // is hidden behind a factory, so they are wrapped explicitly); other attributed
        // services are picked up automatically.
        builder.Services.AddTracing<IScannerService>()
                        .AddTracing<IAuthenticationService>()
                        .AddTracedServices();

        var app = builder.Build();
        HookUnhandledExceptions(app.Services);
        return app;
    }

    // Anything that escapes the UI is what we most need to see remotely. Log it as
    // Critical (so it exports to SigNoz) and force-flush the trace exporter, because a
    // terminating process won't wait for the batch exporter's timer.
    private static void HookUnhandledExceptions(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Snackbox.Web.Unhandled");
        var tracerProvider = services.GetService<OpenTelemetry.Trace.TracerProvider>();

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            logger.LogCritical(e.ExceptionObject as Exception,
                "Unhandled exception in kiosk (terminating: {IsTerminating})", e.IsTerminating);
            tracerProvider?.ForceFlush(3000);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            logger.LogError(e.Exception, "Unobserved task exception in kiosk");
            e.SetObserved();
        };
    }
}
