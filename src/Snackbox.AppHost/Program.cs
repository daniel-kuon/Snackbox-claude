using Nextended.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

// Add Observability Stack using Docker containers

// Loki for logs
var loki = builder.AddContainer("loki", "grafana/loki", "3.0.0")
    .WithHttpEndpoint(port: 3100, targetPort: 3100, name: "http")
    .WithBindMount("./loki-config.yaml", "/etc/loki/local-config.yaml")
    .WithArgs("-config.file=/etc/loki/local-config.yaml");

// Tempo for traces
var tempo = builder.AddContainer("tempo", "grafana/tempo", "2.5.0")
    .WithHttpEndpoint(port: 3200, targetPort: 3200, name: "http")
    .WithHttpEndpoint(port: 4317, targetPort: 4317, name: "otlp-grpc")
    .WithHttpEndpoint(port: 4318, targetPort: 4318, name: "otlp-http")
    .WithBindMount("./tempo-config.yaml", "/etc/tempo.yaml")
    .WithArgs("-config.file=/etc/tempo.yaml");

// Prometheus for metrics
var prometheus = builder.AddContainer("prometheus", "prom/prometheus", "v2.54.1")
    .WithHttpEndpoint(port: 9090, targetPort: 9090, name: "http")
    .WithBindMount("./prometheus.yml", "/etc/prometheus/prometheus.yml")
    .WithArgs("--config.file=/etc/prometheus/prometheus.yml", "--enable-feature=otlp-write-receiver");

// Grafana for visualization
var grafana = builder.AddContainer("grafana", "grafana/grafana", "11.3.0")
    .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "http")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ENABLED", "true")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ORG_ROLE", "Admin")
    .WithEnvironment("GF_AUTH_DISABLE_LOGIN_FORM", "true");

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
    .WaitFor(postgres)
    .WaitFor(tempo)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://tempo:4317")
    .WithExternalHttpEndpoints();

// Add Blazor Server web application
var web = builder.AddProject<Projects.Snackbox_BlazorServer>("web")
    .WithReference(api)
    .WaitFor(api)
    .WaitFor(tempo)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://tempo:4317")
    .WithExternalHttpEndpoints();

// Note: Windows native MAUI app should be run separately from Visual Studio/Rider
// Run using: dotnet run --project src/Snackbox.Web -f net10.0-windows10.0.19041.0

var nativeApp = builder.AddExecutable("native-app", "dotnet", workingDirectory: "../Snackbox.Web")
    .WithArgs(["run", "-f", "net10.0-windows10.0.19041.0"])
    .WithReference(api)
    .WaitFor(api);

// Add a custom resource for database reset using dotnet ef commands
// This will appear in the Aspire dashboard and can be started manually
// Note: Working directory is set to the API project directory for proper EF Core context
var resetDb = builder.AddExecutable("reset-db", "cmd", workingDirectory: "../Snackbox.Api")
    .WithArgs(["/c", "dotnet ef database drop --force && dotnet ef database update"])
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .ExcludeFromManifest();

builder.Build().EnsureDockerRunningIfLocalDebug().Run();
