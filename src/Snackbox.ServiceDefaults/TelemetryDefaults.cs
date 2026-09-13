using System.Diagnostics;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Snackbox.ServiceDefaults;

public enum UserIdentityMode
{
    Pii,
    UserId
}

public sealed class TelemetryOptions
{
    public UserIdentityMode UserIdentityMode { get; set; } = UserIdentityMode.Pii;
    public OtlpOptions Otlp { get; set; } = new();
}

public sealed class OtlpOptions
{
    public string[] AdditionalGrpcEndpoints { get; set; } = Array.Empty<string>();
    public string[] AdditionalHttpEndpoints { get; set; } = Array.Empty<string>();
}

public sealed class TelemetryInstrumentationOptions
{
    public required string ServiceName { get; init; }
    public bool EnableAspNetCoreInstrumentation { get; init; } = true;
    public bool EnableHttpClientInstrumentation { get; init; } = true;
}

public static class TelemetrySources
{
    public const string Ui = "Snackbox.Ui";
    public const string Api = "Snackbox.Api";
    /// <summary>Spans produced by the [Traced] attribute interceptor (see Tracing/TracingProxy.cs).</summary>
    public const string Traced = "Snackbox.Traced";
}

/// <summary>
/// Npgsql emits the stable database semantic conventions (db.system.name, db.namespace,
/// db.query.text). SigNoz - like most tooling built before those were stabilised - looks for
/// the older db.system / db.name / db.statement, so database calls were recorded but never
/// recognised as such: no database node in the service map and nothing under Database Calls.
/// Copy the values across so spans carry both spellings.
/// </summary>
internal sealed class DatabaseSemanticCompatibilityProcessor : BaseProcessor<Activity>
{
    private static readonly (string From, string To)[] Aliases =
    [
        ("db.system.name", "db.system"),
        ("db.namespace", "db.name"),
        ("db.query.text", "db.statement")
    ];

    public override void OnEnd(Activity activity)
    {
        foreach (var (from, to) in Aliases)
        {
            if (activity.GetTagItem(to) != null)
                continue;

            var value = activity.GetTagItem(from);
            if (value != null)
                activity.SetTag(to, value);
        }
    }
}

public static class ServiceDefaultsExtensions
{
    public static IServiceCollection AddSnackboxOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        TelemetryInstrumentationOptions instrumentationOptions)
    {
        var telemetryOptions = GetTelemetryOptions(configuration);
        services.Configure<TelemetryOptions>(configuration.GetSection("Telemetry"));

        var resourceBuilder = CreateResource(instrumentationOptions.ServiceName);

        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.SetResourceBuilder(resourceBuilder)
                    .AddSource(TelemetrySources.Ui, TelemetrySources.Api, TelemetrySources.Traced)
                    .AddSource("Npgsql", "Microsoft.EntityFrameworkCore");

                if (instrumentationOptions.EnableAspNetCoreInstrumentation)
                {
                    tracing.AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            EnrichWithUser(activity, request.HttpContext.User, telemetryOptions.UserIdentityMode);
                        };
                    });
                }

                if (instrumentationOptions.EnableHttpClientInstrumentation)
                {
                    tracing.AddHttpClientInstrumentation(options => { options.RecordException = true; });
                }

                // Make Npgsql's stable db.* attributes recognisable to SigNoz
                tracing.AddProcessor(new DatabaseSemanticCompatibilityProcessor());

                // The default exporter targets OTEL_EXPORTER_OTLP_ENDPOINT (set by the Aspire
                // dashboard for hosted projects). Without it, it would fall back to
                // localhost:4317 and duplicate the SigNoz endpoint configured below.
                if (HasDefaultOtlpEndpoint())
                {
                    tracing.AddOtlpExporter();
                }
                AddAdditionalOtlpExporters(tracing, telemetryOptions);
            });

        return services;
    }

    private static bool HasDefaultOtlpEndpoint() =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));

    public static ILoggingBuilder AddSnackboxOpenTelemetryLogging(
        this ILoggingBuilder logging,
        IConfiguration configuration,
        string serviceName)
    {
        var telemetryOptions = GetTelemetryOptions(configuration);

        logging.AddFilter<OpenTelemetryLoggerProvider>(string.Empty, LogLevel.Information);
        logging.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(CreateResource(serviceName));
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.ParseStateValues = true;

            if (HasDefaultOtlpEndpoint())
            {
                options.AddOtlpExporter();
            }
            AddAdditionalOtlpExporters(options, telemetryOptions);
        });

        return logging;
    }

    private static TelemetryOptions GetTelemetryOptions(IConfiguration configuration)
    {
        var options = new TelemetryOptions();
        configuration.GetSection("Telemetry").Bind(options);
        return options;
    }

    private static ResourceBuilder CreateResource(string serviceName)
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
        return ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: version, serviceInstanceId: Environment.MachineName);
    }

    private static void AddAdditionalOtlpExporters(TracerProviderBuilder tracing, TelemetryOptions telemetryOptions)
    {
        foreach (var endpoint in NormalizeEndpoints(telemetryOptions.Otlp.AdditionalGrpcEndpoints))
        {
            tracing.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(endpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
            });
        }

        foreach (var endpoint in NormalizeEndpoints(telemetryOptions.Otlp.AdditionalHttpEndpoints))
        {
            tracing.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(endpoint);
                options.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
        }
    }

    private static void AddAdditionalOtlpExporters(OpenTelemetryLoggerOptions loggingOptions, TelemetryOptions telemetryOptions)
    {
        foreach (var endpoint in NormalizeEndpoints(telemetryOptions.Otlp.AdditionalGrpcEndpoints))
        {
            loggingOptions.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(endpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
            });
        }

        foreach (var endpoint in NormalizeEndpoints(telemetryOptions.Otlp.AdditionalHttpEndpoints))
        {
            loggingOptions.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(endpoint);
                options.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
        }
    }

    private static IEnumerable<string> NormalizeEndpoints(IEnumerable<string>? endpoints)
    {
        if (endpoints == null)
        {
            yield break;
        }

        foreach (var endpoint in endpoints)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                continue;
            }

            yield return endpoint.Trim();
        }
    }

    private static void EnrichWithUser(Activity activity, ClaimsPrincipal user, UserIdentityMode identityMode)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return;
        }

        if (identityMode == UserIdentityMode.UserId)
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                activity.SetTag("user.id", userId);
            }

            return;
        }

        var username = user.Identity?.Name ?? user.FindFirst(ClaimTypes.Name)?.Value;
        if (!string.IsNullOrWhiteSpace(username))
        {
            activity.SetTag("user.username", username);
            activity.SetTag("user.full_name", username);
        }

        var email = user.FindFirst(ClaimTypes.Email)?.Value;
        if (!string.IsNullOrWhiteSpace(email))
        {
            activity.SetTag("user.email", email);
        }
    }
}
