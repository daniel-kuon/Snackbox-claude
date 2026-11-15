using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Snackbox.ApiClient;
using Snackbox.Components.Services;
using Snackbox.Web.Services;
using Snackbox.ServiceDefaults;
using OpenTelemetry;
using OpenTelemetry.Logs;
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

        // Optional appsettings.json next to the executable, so settings like
        // Window:StartMinimized can be flipped on the kiosk without a rebuild.
        var externalSettings = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(externalSettings))
        {
            builder.Configuration.AddJsonFile(externalSettings, optional: true, reloadOnChange: false);
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

    private static ILogger? _crashLogger;
    private static TracerProvider? _tracerProvider;
    private static LoggerProvider? _loggerProvider;

    // Anything that escapes the UI is what we most need to see remotely. Logged as Critical
    // (so it exports to SigNoz) and both exporters are flushed - a terminating process does not
    // wait for the batch exporters' timers. Only the traces used to be flushed, so the crash's
    // own log record died with the process. The start and the exit are logged too: a start
    // without an exit before it is a crash that no handler saw (native, WebView2, fail-fast).
    private static void HookUnhandledExceptions(IServiceProvider services)
    {
        _crashLogger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Snackbox.Web.Unhandled");
        _tracerProvider = services.GetService<TracerProvider>();
        _loggerProvider = services.GetService<LoggerProvider>();

        _crashLogger.LogInformation("Kiosk started (version {Version}, pid {Pid})",
            Assembly.GetExecutingAssembly().GetName().Version, Environment.ProcessId);

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            ReportCrash(e.ExceptionObject as Exception, $"AppDomain, terminating: {e.IsTerminating}");

        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            _crashLogger.LogInformation("Kiosk exiting");
            Flush();
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            _crashLogger.LogError(e.Exception, "Unobserved task exception in kiosk");
            e.SetObserved();
        };
    }

    /// <summary>
    /// Also called by WinUI's own UnhandledException (Platforms/Windows/App.xaml.cs): an exception
    /// on the UI thread reaches that one, and often never the AppDomain's.
    /// </summary>
    public static void ReportCrash(Exception? exception, string source)
    {
        // First the file: it needs no network, and the exporters may not get out in time
        try
        {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Snackbox", "logs");
            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, "kiosk-crash.log"),
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {source}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Reporting a crash must not cause another one
        }

        _crashLogger?.LogCritical(exception, "Unhandled exception in kiosk ({Source})", source);
        Flush();
    }

    private static void Flush()
    {
        _tracerProvider?.ForceFlush(3000);
        _loggerProvider?.ForceFlush(3000);
    }
}
