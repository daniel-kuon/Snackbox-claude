using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Snackbox.ServiceDefaults;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddOpenTelemetryConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        string serviceVersion = "1.0.0")
    {
        // Get OTLP endpoint from configuration (set by Aspire)
        var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] 
            ?? configuration.GetConnectionString("otel") 
            ?? "http://localhost:4317";

        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development",
                ["service.namespace"] = "Snackbox"
            });

        // Configure OpenTelemetry
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development",
                    ["service.namespace"] = "Snackbox"
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = (httpContext) => true; // Capture all requests
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddSource(serviceName) // Add custom activity source
                    .SetSampler(new AlwaysOnSampler()) // Sample everything for low-volume scenarios
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(serviceName) // Add custom meter
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    });
            });

        return services;
    }

    public static ILoggingBuilder AddOpenTelemetryLogging(
        this ILoggingBuilder logging,
        IConfiguration configuration,
        string serviceName)
    {
        var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] 
            ?? configuration.GetConnectionString("otel") 
            ?? "http://localhost:4317";

        logging.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName)
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["deployment.environment"] = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development",
                        ["service.namespace"] = "Snackbox"
                    }));
            
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.ParseStateValues = true;
            
            options.AddOtlpExporter(exporter =>
            {
                exporter.Endpoint = new Uri(otlpEndpoint);
                exporter.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            });
        });

        return logging;
    }
}
