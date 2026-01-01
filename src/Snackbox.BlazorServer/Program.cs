using Snackbox.BlazorServer.Components;
using Snackbox.BlazorServer.Services;
using Snackbox.Components.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Localization services
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Register storage service for web
builder.Services.AddSingleton<IStorageService, WebStorageService>();

// Localization state service for dynamic language switching
builder.Services.AddSingleton<Snackbox.Components.Services.ILocalizationService, Snackbox.Components.Services.LocalizationService>();

// Register HttpClient for API calls
var apiUrl = builder.Configuration["API_HTTPS"] ?? builder.Configuration["API_HTTP"] ?? throw new InvalidOperationException("API URL is not configured.");
builder.Services.AddHttpClient<IAuthenticationService, AuthenticationService>(client =>
{
    client.BaseAddress = new Uri(apiUrl);
});

// Register scanner service with HttpClient for Windows
builder.Services.AddHttpClient<IScannerService, ScannerService>(client =>
{
    client.BaseAddress = new Uri(apiUrl);
});

// Add HttpClient for other services
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

// Configure request localization
var supportedCultures = new[] { "en", "de" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);
app.UseRequestLocalization(localizationOptions);

app.MapStaticAssets();
app.MapRazorComponents<App>()
   .AddAdditionalAssemblies(typeof(Snackbox.Components.Pages.Login).Assembly)
    .AddInteractiveServerRenderMode();

app.Run();
