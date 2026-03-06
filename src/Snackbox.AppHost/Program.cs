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

// Email adapter parameters
var emailEnabled = builder.AddParameter("email-enabled", "false");
var emailSmtpServer = builder.AddParameter("email-smtp-server", "smtp.gmail.com");
var emailSmtpPort = builder.AddParameter("email-smtp-port", "587");
var emailEnableSsl = builder.AddParameter("email-enable-ssl", "true");
var emailUsername = builder.AddParameter("email-username", "your-email@gmail.com");
var emailPassword = builder.AddParameter("email-password", secret: true);
var emailFromEmail = builder.AddParameter("email-from-email", "noreply@snackbox.example.com");
var emailFromName = builder.AddParameter("email-from-name", "Snackbox");
var emailPaypalLink = builder.AddParameter("email-paypal-link", "https://paypal.me/yourpaypallink");
var backupEmailRecipient = builder.AddParameter("backup-email-recipient", "admin@example.com");
var searchUpcDataApiKey = builder.AddParameter("searchupcdata-api-key", secret: true);

// Add API project with Swagger UI available at /swagger
var api = builder.AddProject<Snackbox_Api>("api")
                 .WithReference(postgres)
                 .WaitFor(postgres)
                 .WithExternalHttpEndpoints()
                 .WithEnvironment("EmailSettings__Enabled", emailEnabled)
                 .WithEnvironment("EmailSettings__SmtpServer", emailSmtpServer)
                 .WithEnvironment("EmailSettings__SmtpPort", emailSmtpPort)
                 .WithEnvironment("EmailSettings__EnableSsl", emailEnableSsl)
                 .WithEnvironment("EmailSettings__Username", emailUsername)
                 .WithEnvironment("EmailSettings__Password", emailPassword)
                 .WithEnvironment("EmailSettings__FromEmail", emailFromEmail)
                 .WithEnvironment("EmailSettings__FromName", emailFromName)
                 .WithEnvironment("EmailSettings__PayPalLink", emailPaypalLink)
                 .WithEnvironment("Backup__EmailRecipient", backupEmailRecipient)
                 .WithEnvironment("SearchUpcData__ApiKey", searchUpcDataApiKey);

// Add Blazor Server web application
// ReSharper disable once UnusedVariable
var web = builder.AddProject<Snackbox_BlazorServer>("web").WithReference(api).WithExternalHttpEndpoints();

// Note: Windows native MAUI app should be run separately from Visual Studio/Rider
// Run using: dotnet run --project src/Snackbox.Web -f net10.0-windows10.0.19041.0

// ReSharper disable once UnusedVariable
var nativeApp = builder.AddExecutable("native-app", "dotnet", workingDirectory: "../Snackbox.Web")
                       .WithArgs("run", "-f", "net10.0-windows10.0.19041.0")
                       .WithReference(api)
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
