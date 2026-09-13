using Microsoft.Extensions.DependencyInjection;
using Nextended.Aspire;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// SigNoz (self-hosted observability) receives OTLP on 4317 (gRPC) / 4318 (HTTP).
// All services export there in addition to the Aspire dashboard's own OTLP endpoint.
const string otelCollectorGrpcEndpoint = "http://localhost:4317";

IResourceBuilder<ParameterResource> postgresPassword =
    builder.AddParameter("postgresspassword",
                         "postgresspassword",
                         publishValueAsDefault: false,
                         secret: true);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres", password: postgresPassword)
                      .WithContainerName("snackbox-postgres")
                      .WithLifetime(ContainerLifetime.Persistent)
                      // Fixed host ports live in the registered range, not the ephemeral
                      // range (49152-65535) where Windows/Hyper-V dynamically reserves blocks
                      // and would refuse the bind ("access to socket forbidden").
                      .WithHostPort(15653)
                      .WithDataVolume()
                      .WithPgAdmin(b => b.WithContainerName("snackbox-pgadmin")
                                         .WithHostPort(15654)
                                         .WithLifetime(ContainerLifetime.Persistent))
                      .AddDatabase("snackboxdb");

// SigNoz stack (ClickHouse, collector, UI on http://localhost:3301) via its own compose file.
// `up -d` returns immediately, so this resource simply shows as finished once the
// containers are started; they keep running independently of the AppHost.
// ReSharper disable once UnusedVariable
var signozCompose = builder.AddExecutable("signoz", "docker", workingDirectory: "./Signoz")
                           .WithArgs("compose", "-f", "docker-compose.yaml", "up", "-d")
                           .ExcludeFromManifest();

// Add API project with Swagger UI available at /swagger
var api = builder.AddProject<Snackbox_Api>("api").WithReference(postgres).WaitFor(postgres).WithExternalHttpEndpoints();
api.WithEnvironment("Telemetry__Otlp__AdditionalGrpcEndpoints__0", otelCollectorGrpcEndpoint);

// Add Blazor Server web application
// ReSharper disable once UnusedVariable
var web = builder.AddProject<Snackbox_BlazorServer>("web").WithReference(api).WithExternalHttpEndpoints();
web.WithEnvironment("Telemetry__Otlp__AdditionalGrpcEndpoints__0", otelCollectorGrpcEndpoint);
web.WithEnvironment("Telemetry__SignozUrl", "http://localhost:3301");

// Note: Windows native MAUI app should be run separately from Visual Studio/Rider
// Run using: dotnet run --project src/Snackbox.Web -f net10.0-windows10.0.19041.0

// ReSharper disable once UnusedVariable
var nativeApp = builder.AddExecutable("native-app", "dotnet", workingDirectory: "../Snackbox.Web")
                       .WithArgs("run", "-f", "net10.0-windows10.0.19041.0")
                       .WithReference(api)
                       .WithEnvironment("Telemetry__Otlp__AdditionalGrpcEndpoints__0", otelCollectorGrpcEndpoint)
                       .WithExplicitStartIf(builder.ExecutionContext.IsRunMode);

// Add a custom resource for database reset using dotnet ef commands
// This will appear in the Aspire dashboard and can be started manually
// Note: Working directory is set to the API project directory for proper EF Core context
// ReSharper disable once UnusedVariable
var resetDb = builder.AddExecutable("reset-db", "cmd", workingDirectory: "../Snackbox.Api")
                     .WithArgs("/c", "dotnet ef database drop --force && dotnet ef database update")
                     .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
                     .WithExplicitStart()
                     .ExcludeFromManifest();

builder.Build().EnsureDockerRunningIfLocalDebug().Run();
