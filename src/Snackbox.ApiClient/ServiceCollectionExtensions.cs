using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Snackbox.ApiClient;

/// <summary>
/// Extension methods for registering Snackbox API clients with dependency injection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Snackbox API clients with the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="baseUrl">The base URL of the Snackbox API</param>
    /// <param name="configureClient">Optional delegate to configure the HttpClient</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSnackboxApiClient(
        this IServiceCollection services,
        string baseUrl,
        Action<HttpClient>? configureClient = null)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        jsonOptions.Converters.Add(new JsonStringEnumConverter());

        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(jsonOptions)
        };

        void AddRefitClient<TApi>() where TApi : class
        {
            services.AddRefitClient<TApi>(refitSettings)
                    .ConfigureHttpClient(c =>
                                         {
                                             c.BaseAddress = new Uri(baseUrl);
                                             configureClient?.Invoke(c);
                                         });
        }

        // Register all API clients with authentication handler
        AddRefitClient<IAuthApi>();
        AddRefitClient<IProductsApi>();
        AddRefitClient<IUsersApi>();
        AddRefitClient<IPaymentsApi>();
        AddRefitClient<IPurchasesApi>();
        AddRefitClient<IProductBatchesApi>();
        AddRefitClient<IShelvingActionsApi>();
        AddRefitClient<IScannerApi>();
        AddRefitClient<IBarcodesApi>();
        AddRefitClient<IBarcodeLookupApi>();
        AddRefitClient<IBackupApi>();
        AddRefitClient<IInvoicesApi>();
        AddRefitClient<ISettingsApi>();

        return services;
    }

    /// <summary>
    /// Registers all Snackbox API clients with authentication support
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="baseUrl">The base URL of the Snackbox API</param>
    /// <param name="configureClient">Optional delegate to configure the HttpClient</param>
    /// <returns>IHttpClientBuilder for the last registered client (for chaining handlers)</returns>
    public static void AddSnackboxApiClientWithAuth<THandler>(
        this IServiceCollection services,
        string baseUrl,
        Action<HttpClient>? configureClient = null)
        where THandler : DelegatingHandler
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        jsonOptions.Converters.Add(new JsonStringEnumConverter());

        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(jsonOptions)
        };

        void AddRefitClient<TApi>() where TApi : class
        {
            services.AddRefitClient<TApi>(refitSettings)
                    .ConfigureHttpClient(c =>
                                         {
                                             c.BaseAddress = new Uri(baseUrl);
                                             // Disable HTTP caching to avoid stale data
                                             c.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
                                             {
                                                 NoCache = true,
                                                 NoStore = true,
                                                 MustRevalidate = true
                                             };
                                             c.DefaultRequestHeaders.Pragma.Add(new System.Net.Http.Headers.NameValueHeaderValue("no-cache"));
                                             configureClient?.Invoke(c);
                                         })
                    .AddHttpMessageHandler<THandler>();
        }

        // Register all API clients with authentication handler
        services.AddRefitClient<IAuthApi>(refitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                configureClient?.Invoke(c);
            });
        // Auth API doesn't need auth handler

        AddRefitClient<IProductsApi>();
        AddRefitClient<IUsersApi>();
        AddRefitClient<IPaymentsApi>();
        AddRefitClient<IPurchasesApi>();
        AddRefitClient<IProductBatchesApi>();
        AddRefitClient<IShelvingActionsApi>();
        AddRefitClient<IScannerApi>();
        AddRefitClient<IBarcodesApi>();
        AddRefitClient<IBarcodeLookupApi>();
        AddRefitClient<IInvoicesApi>();
        AddRefitClient<IBackupApi>();
        AddRefitClient<ISettingsApi>();
    }

}
