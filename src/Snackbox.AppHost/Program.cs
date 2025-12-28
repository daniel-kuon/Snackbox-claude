using Nextended.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

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

// Add API project
var api = builder.AddProject<Projects.Snackbox_Api>("api")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithExternalHttpEndpoints();

// Note: MAUI Web project should be run separately as it cannot be orchestrated by Aspire
// Run it from Visual Studio/Rider or using: dotnet run --project src/Snackbox.Web -f net10.0-windows10.0.19041.0

// Add a custom resource for database reset using dotnet ef commands
// This will appear in the Aspire dashboard and can be started manually
// Note: Working directory is set to the API project directory for proper EF Core context
var resetDb = builder.AddExecutable("reset-db", "cmd", workingDirectory: "../Snackbox.Api")
    .WithArgs(["/c", "dotnet ef database drop --force && dotnet ef database update"])
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .ExcludeFromManifest();

builder.Build().EnsureDockerRunningIfLocalDebug().Run();
