using Microsoft.Extensions.DependencyInjection;
using Nextended.Aspire;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ParameterResource> postgresPassword =
    builder.AddParameter("postgresspassword",
                         "postgresspassword",
                         publishValueAsDefault: false,
                         secret: true);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres", password: postgresPassword)
                      .WithContainerName("snackbox-postgres")
                      .WithLifetime(ContainerLifetime.Persistent)
                      .WithHostPort(59653)
                      .WithDataVolume()
                      .WithPgAdmin(b => b.WithContainerName("snackbox-pgadmin")
                                         .WithHostPort(59654)
                                         .WithLifetime(ContainerLifetime.Persistent))
                      .AddDatabase("snackboxdb");

// Add API project with Swagger UI available at /swagger
var api = builder.AddProject<Snackbox_Api>("api").WithReference(postgres).WaitFor(postgres).WithExternalHttpEndpoints();

// Add Blazor Server web application
// ReSharper disable once UnusedVariable
var web = builder.AddProject<Snackbox_BlazorServer>("web").WithReference(api).WithExternalHttpEndpoints();

// Note: Windows native MAUI app should be run separately from Visual Studio/Rider
// Run using: dotnet run --project src/Snackbox.Web -f net10.0-windows10.0.19041.0

// ReSharper disable once UnusedVariable
var nativeApp = builder.AddExecutable("native-app", "dotnet", workingDirectory: "../Snackbox.Web")
                       .WithArgs("run", "-f", "net10.0-windows10.0.19041.0")
                       .WithReference(api);

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
