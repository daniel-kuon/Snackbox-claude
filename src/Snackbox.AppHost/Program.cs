using Nextended.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

// Add Observability Stack (Grafana, Loki, Tempo, Mimir)
var grafana = builder.AddGrafana("grafana")
    .WithDataVolume();

var loki = builder.AddLoki("loki")
    .WithDataVolume();

var tempo = builder.AddTempo("tempo")
    .WithDataVolume();

var mimir = builder.AddMimir("mimir")
    .WithDataVolume();

// Connect Grafana to data sources
grafana
    .WithDataSource(loki)
    .WithDataSource(tempo)
    .WithDataSource(mimir);

IResourceBuilder<ParameterResource>? postgresPassword =
    builder.AddParameter("postgresspassword",
                         "postgresspassword",
                         publishValueAsDefault: false,
                         secret: true);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithDataVolume()
    .WithPgAdmin()
    .AddDatabase("snackboxdb");

// Add API project with Swagger UI available at /swagger
var api = builder.AddProject<Projects.Snackbox_Api>("api")
    .WithReference(postgres)
    .WithReference(loki)
    .WithReference(tempo)
    .WithReference(mimir)
    .WaitFor(postgres)
    .WithExternalHttpEndpoints();

// Add Blazor Server web application
var web = builder.AddProject<Projects.Snackbox_BlazorServer>("web")
    .WithReference(api)
    .WithReference(loki)
    .WithReference(tempo)
    .WithReference(mimir)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

// Note: Windows native MAUI app should be run separately from Visual Studio/Rider
// Run using: dotnet run --project src/Snackbox.Web -f net10.0-windows10.0.19041.0

var windowsMaui = builder.AddExecutable("maui-windows",
    "dotnet",
    workingDirectory: "../Snackbox.Web")
    .WithArgs(["run", "-f", "net10.0-windows10.0.19041.0"])
    .WithReference(api)
    .WaitFor(api)
    .ExcludeFromManifest(); // Exclude from manifest as it's not cross-platform

// Add a custom resource for database reset using dotnet ef commands
// This will appear in the Aspire dashboard and can be started manually
// Note: Working directory is set to the API project directory for proper EF Core context
var resetDb = builder.AddExecutable("reset-db", "cmd", workingDirectory: "../Snackbox.Api")
    .WithArgs(["/c", "dotnet ef database drop --force && dotnet ef database update"])
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .ExcludeFromManifest();

builder.Build().EnsureDockerRunningIfLocalDebug().Run();
