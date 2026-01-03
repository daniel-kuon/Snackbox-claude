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
        var refitSettings = new RefitSettings();
        
        // Register all API clients
        services.AddRefitClient<IAuthApi>(refitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                configureClient?.Invoke(c);
            });
            
        services.AddRefitClient<IProductsApi>(refitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                configureClient?.Invoke(c);
            });
            
        services.AddRefitClient<IUsersApi>(refitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                configureClient?.Invoke(c);
            });
            
        services.AddRefitClient<IPaymentsApi>(refitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                configureClient?.Invoke(c);
            });
            
        services.AddRefitClient<IPurchasesApi>(refitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                configureClient?.Invoke(c);
            });
            
        services.AddRefitClient<IProductBatchesApi>(refitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                configureClient?.Invoke(c);
            });
            
        services.AddRefitClient<IShelvingActionsApi>(refitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                configureClient?.Invoke(c);
            });
            
        services.AddRefitClient<IScannerApi>(refitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                configureClient?.Invoke(c);
            });
            
        services.AddRefitClient<IBarcodesApi>(refitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                configureClient?.Invoke(c);
            });
            
        services.AddRefitClient<IBarcodeLookupApi>(refitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                configureClient?.Invoke(c);
            });
            
        return services;
    }
    
    /// <summary>
    /// Registers all Snackbox API clients with authentication support
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="baseUrl">The base URL of the Snackbox API</param>
    /// <param name="configureClient">Optional delegate to configure the HttpClient</param>
    /// <returns>IHttpClientBuilder for the last registered client (for chaining handlers)</returns>
    public static IHttpClientBuilder AddSnackboxApiClientWithAuth<THandler>(
        this IServiceCollection services, 
        string baseUrl,
        Action<HttpClient>? configureClient = null)
        where THandler : DelegatingHandler
    {
        var refitSettings = new RefitSettings();
        
        // Register all API clients with authentication handler
        services.AddRefitClient<IAuthApi>(refitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                configureClient?.Invoke(c);
            });
        // Auth API doesn't need auth handler
            
        services.AddRefitClient<IProductsApi>(refitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                configureClient?.Invoke(c);
            })
            .AddHttpMessageHandler<THandler>();
            
        services.AddRefitClient<IUsersApi>(refitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                configureClient?.Invoke(c);
            })
            .AddHttpMessageHandler<THandler>();
            
        services.AddRefitClient<IPaymentsApi>(refitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                configureClient?.Invoke(c);
            })
            .AddHttpMessageHandler<THandler>();
            
        services.AddRefitClient<IPurchasesApi>(refitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                configureClient?.Invoke(c);
            })
            .AddHttpMessageHandler<THandler>();
            
        services.AddRefitClient<IProductBatchesApi>(refitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                configureClient?.Invoke(c);
            })
            .AddHttpMessageHandler<THandler>();
            
        services.AddRefitClient<IShelvingActionsApi>(refitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                configureClient?.Invoke(c);
            })
            .AddHttpMessageHandler<THandler>();
            
        services.AddRefitClient<IScannerApi>(refitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                configureClient?.Invoke(c);
            })
            .AddHttpMessageHandler<THandler>();
            
        services.AddRefitClient<IBarcodesApi>(refitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                configureClient?.Invoke(c);
            })
            .AddHttpMessageHandler<THandler>();
            
        return services.AddRefitClient<IBarcodeLookupApi>(refitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                configureClient?.Invoke(c);
            })
            .AddHttpMessageHandler<THandler>();
    }
}
