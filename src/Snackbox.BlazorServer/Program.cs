using System.Security.Cryptography.X509Certificates;
using Snackbox.ApiClient;
using Snackbox.BlazorServer.Components;
using Snackbox.BlazorServer.Services;
using Snackbox.Components.Pages;
using Snackbox.Components.Services;
using Snackbox.ServiceDefaults;
using Snackbox.ServiceDefaults.Tracing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSnackboxOpenTelemetry(builder.Configuration, new TelemetryInstrumentationOptions
{
    ServiceName = "snackbox-blazor",
    EnableAspNetCoreInstrumentation = true,
    EnableHttpClientInstrumentation = true
});
builder.Logging.AddSnackboxOpenTelemetryLogging(builder.Configuration, "snackbox-blazor");

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Register storage service for web
builder.Services.AddSingleton<IStorageService, WebStorageService>()
       .AddSingleton<IScannerListener, DummyScannerListener>();

builder.Services.AddScoped<IUiTelemetry, UiTelemetry>();

// Register Snackbar service
builder.Services.AddScoped<SnackbarService>();
builder.Services.AddSingleton<AppStartupState>();

// Register delegating handler for authentication
builder.Services.AddTransient<AuthenticationHeaderHandler>();

// Register HttpClient for API calls
var apiUrl = builder.Configuration["API_HTTPS"] ??
             builder.Configuration["API_HTTP"] ?? throw new InvalidOperationException("API URL is not configured.");

// Register all Snackbox API clients with authentication
builder.Services.AddSnackboxApiClientWithAuth<AuthenticationHeaderHandler>(apiUrl);

builder.Services.AddHttpClient<IAuthenticationService, AuthenticationService>(client =>
                                                                              {
                                                                                  client.BaseAddress = new Uri(apiUrl);
                                                                              })
       .AddHttpMessageHandler<AuthenticationHeaderHandler>();

// Register scanner service with HttpClient for Windows
builder.Services.AddHttpClient<IScannerService, ScannerService>(client => { client.BaseAddress = new Uri(apiUrl); })
       .AddHttpMessageHandler<AuthenticationHeaderHandler>();

// Add default HttpClient with BaseAddress and authentication handler
builder.Services.AddHttpClient("DefaultClient", client => { client.BaseAddress = new Uri(apiUrl); })
       .AddHttpMessageHandler<AuthenticationHeaderHandler>();

// Also add a default unnamed HttpClient
builder.Services.AddHttpClient("", client => { client.BaseAddress = new Uri(apiUrl); })
       .AddHttpMessageHandler<AuthenticationHeaderHandler>();

// For components that still use AddScoped<HttpClient> or inject HttpClient directly
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient(""));

// Development-only UI test helper (API only answers in Development with TestHelper:Enabled)
builder.Services.AddHttpClient<TestHelperClient>(client => { client.BaseAddress = new Uri(apiUrl); });

// [Traced] interception (typed HttpClients wrapped explicitly, the rest auto-discovered)
builder.Services.AddTracing<IScannerService>()
                .AddTracing<IAuthenticationService>()
                .AddTracedServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// /install must stay reachable over plain HTTP: a phone cannot load the HTTPS
// site until it has installed the self-signed certificate offered on that page.
app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/install"),
            b => b.UseHttpsRedirection());

app.UseAntiforgery();

app.MapStaticAssets();

// Public part of the server certificate, for installing on phones.
// Reads the same certificate Kestrel serves (Kestrel:Certificates:Default).
app.MapGet("/install/certificate", (IConfiguration config) =>
{
    var certPath = config["Kestrel:Certificates:Default:Path"];
    if (string.IsNullOrWhiteSpace(certPath) || !File.Exists(certPath))
        return Results.NotFound("No certificate configured. Set Kestrel:Certificates:Default:Path in appsettings.json.");

    var password = config["Kestrel:Certificates:Default:Password"];
    var extension = Path.GetExtension(certPath).ToLowerInvariant();
    using var cert = extension is ".pfx" or ".p12"
        ? X509CertificateLoader.LoadPkcs12FromFile(certPath, password)
        : X509CertificateLoader.LoadCertificateFromFile(certPath);

    // DER-encoded public certificate only - never the private key
    return Results.File(cert.Export(X509ContentType.Cert), "application/x-x509-ca-cert", "snackbox.crt");
});

// Plain-HTML install guide (works before the certificate is trusted)
app.MapGet("/install", (HttpContext ctx) =>
{
    var host = ctx.Request.Host.Host;
    var httpsUrl = $"https://{host}";
    var html = $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <title>Install Snackbox on your phone</title>
            <style>
                body { font-family: system-ui, sans-serif; max-width: 640px; margin: 0 auto; padding: 1.5rem; color: #222; line-height: 1.5; }
                h1 { font-size: 1.5rem; } h2 { font-size: 1.1rem; margin-top: 1.5rem; }
                ol { padding-left: 1.25rem; } li { margin: 0.5rem 0; }
                .btn { display: inline-block; padding: 0.7rem 1.4rem; background: #0d6efd; color: #fff; border-radius: 8px; text-decoration: none; font-weight: 600; }
                .note { background: #fff3cd; padding: 0.75rem 1rem; border-radius: 8px; font-size: 0.9rem; }
                code { background: #f1f3f5; padding: 0.1rem 0.35rem; border-radius: 4px; }
            </style>
        </head>
        <body>
            <h1>📲 Install Snackbox on your phone</h1>
            <p>Snackbox runs only inside the office Wi-Fi and uses a self-signed security
               certificate. Install the certificate once, then add the app to your home screen.</p>

            <h2>Step 1 - Install the certificate</h2>
            <p><a class="btn" href="/install/certificate">⬇ Download certificate (snackbox.crt)</a></p>
            <ol>
                <li><strong>Android:</strong> open the downloaded file, or go to
                    <em>Settings → Security &amp; privacy → More security settings → Install from device storage → CA certificate</em>
                    and pick <code>snackbox.crt</code>.</li>
                <li><strong>iPhone:</strong> download the file and confirm the profile, then go to
                    <em>Settings → General → VPN &amp; Device Management</em> and install it. Afterwards enable it under
                    <em>Settings → General → About → Certificate Trust Settings</em>.</li>
            </ol>

            <h2>Step 2 - Add the app to your home screen</h2>
            <ol>
                <li>Open <a href="{{httpsUrl}}">{{httpsUrl}}</a> in your phone's browser.</li>
                <li><strong>Android (Chrome):</strong> tap the ⋮ menu → <em>Add to Home screen</em> → <em>Install</em>.</li>
                <li><strong>iPhone (Safari):</strong> tap the share button → <em>Add to Home Screen</em>.</li>
            </ol>

            <p class="note">If the address above doesn't open, ask your admin for the correct
               address and port of the Snackbox server.</p>
        </body>
        </html>
        """;
    return Results.Content(html, "text/html");
});
app.MapRazorComponents<App>()
   .AddAdditionalAssemblies(typeof(Login).Assembly)
   .AddInteractiveServerRenderMode();

app.Run();
