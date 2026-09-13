using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Snackbox.ServiceDefaults.Tracing;

public static class TracingServiceCollectionExtensions
{
    /// <summary>
    /// Wraps the registration of <typeparamref name="TInterface"/> in a <see cref="TracingProxy{TInterface}"/>.
    /// Use for services whose implementation type is not visible in the descriptor (typed
    /// HttpClients, factory registrations). The original lifetime is kept.
    /// </summary>
    public static IServiceCollection AddTracing<TInterface>(this IServiceCollection services) where TInterface : class
    {
        var descriptor = services.LastOrDefault(d => d.ServiceType == typeof(TInterface))
            ?? throw new InvalidOperationException($"{typeof(TInterface).Name} must be registered before calling AddTracing.");

        return Wrap<TInterface>(services, descriptor);
    }

    /// <summary>
    /// Finds every interface registration whose implementation type carries <see cref="TracedAttribute"/>
    /// (on the class or any method) and wraps it in a tracing proxy. Call once, after all
    /// registrations. Factory-based registrations cannot be inspected - use <see cref="AddTracing{TInterface}"/>.
    /// </summary>
    public static IServiceCollection AddTracedServices(this IServiceCollection services)
    {
        var candidates = services
            .Where(d => d.ServiceType.IsInterface && !d.ServiceType.IsGenericTypeDefinition)
            .Where(d => IsTraced(d.ImplementationType ?? d.ImplementationInstance?.GetType()))
            .ToList();

        foreach (var descriptor in candidates)
        {
            var wrap = typeof(TracingServiceCollectionExtensions)
                .GetMethod(nameof(Wrap), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(descriptor.ServiceType);
            wrap.Invoke(null, [services, descriptor]);
        }

        return services;
    }

    private static bool IsTraced(Type? implType) =>
        implType != null &&
        (implType.GetCustomAttribute<TracedAttribute>() != null
         || implType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Any(m => m.GetCustomAttribute<TracedAttribute>() != null));

    private static IServiceCollection Wrap<TInterface>(IServiceCollection services, ServiceDescriptor descriptor)
        where TInterface : class
    {
        services.Remove(descriptor);
        services.Add(new ServiceDescriptor(
            typeof(TInterface),
            sp => TracingProxy<TInterface>.Create((TInterface)CreateInstance(sp, descriptor)),
            descriptor.Lifetime));
        return services;
    }

    private static object CreateInstance(IServiceProvider sp, ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance != null)
            return descriptor.ImplementationInstance;
        if (descriptor.ImplementationFactory != null)
            return descriptor.ImplementationFactory(sp);
        return ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType!);
    }
}
