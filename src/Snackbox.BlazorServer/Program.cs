using Snackbox.ApiClient;
using Snackbox.BlazorServer.Components;
using Snackbox.BlazorServer.Services;
using Snackbox.Components.Pages;
using Snackbox.Components.Services;

var builder = WebApplication.CreateBuilder(args);

// Render/Container: if PORT is set, bind Kestrel to 0.0.0.0:PORT (HTTP only)
var portEnv = Environment.GetEnvironmentVariable("PORT");
if (int.TryParse(portEnv, out var port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Register storage service for web
builder.Services.AddSingleton<IStorageService, WebStorageService>()
       .AddSingleton<IScannerListener, DummyScannerListener>();

// Register delegating handler for authentication
builder.Services.AddTransient<AuthenticationHeaderHandler>();

// Register HttpClient for API calls
var apiUrl = builder.Configuration["API_URL"] ??
             builder.Configuration["API_HTTPS"] ??
             builder.Configuration["API_HTTP"] ??
             throw new InvalidOperationException("API URL is not configured. Set API_URL or API_HTTPS/API_HTTP.");

// Register all Snackbox API clients with authentication
builder.Services.AddSnackboxApiClientWithAuth<AuthenticationHeaderHandler>(apiUrl);

builder.Services.AddHttpClient<IAuthenticationService, AuthenticationService>(client =>
                                                                              {
                                                                                  client.BaseAddress = new Uri(apiUrl);
                                                                              });

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // Avoid HSTS on platforms that terminate TLS and expose HTTP internally (e.g., Render with PORT)
    if (string.IsNullOrEmpty(portEnv))
    {
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
// Avoid HTTPS redirection when running on platforms that provide only HTTP (e.g., Render with PORT)
if (string.IsNullOrEmpty(portEnv))
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
   .AddAdditionalAssemblies(typeof(Login).Assembly)
   .AddInteractiveServerRenderMode();

app.Run();
