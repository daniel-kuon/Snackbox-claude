using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Snackbox.Api.Data;
using Snackbox.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Load secrets file if it exists
builder.Configuration.AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: true);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Snackbox API",
        Version = "v1",
        Description = "Snackbox API for managing products, users, purchases, and payments"
    });
});

// Add OpenAPI document generation for build-time client generation
builder.Services.AddOpenApi("v1");

// Configure PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("snackboxdb")
    ?? throw new InvalidOperationException("Database connection string 'snackboxdb' is not configured.");

// Modify connection string to reduce caching issues
var connectionStringBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
{
    // Disable server-side prepared statements to avoid cached query plans
    MaxAutoPrepare = 0,
    // Reduce connection pooling cache
    NoResetOnClose = false
};

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionStringBuilder.ConnectionString);
    // Enable detailed logging in development
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    options.EnableDetailedErrors(builder.Environment.IsDevelopment());
});

// Add health checks with database check
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "database", tags: ["db", "ready"]);

// Register authentication service
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Register achievement service
builder.Services.AddScoped<IAchievementService, AchievementService>();

// Register stock calculation service
builder.Services.AddScoped<IStockCalculationService, StockCalculationService>();

// Register product matching service
builder.Services.AddScoped<IProductMatchingService, ProductMatchingService>();

// Register invoice parser services
builder.Services.AddScoped<IInvoiceParserService, SonderpostenInvoiceParser>();
builder.Services.AddScoped<IInvoiceParserService, SelgrosInvoiceParser>();
builder.Services.AddScoped<IInvoiceParserService, ReweInvoiceParser>();
builder.Services.AddScoped<InvoiceParserFactory>();

// Register email service
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();

// Register product best before date service
builder.Services.AddScoped<IProductBestBeforeDateService, ProductBestBeforeDateService>();

// Register barcode lookup service
builder.Services.AddHttpClient<IBarcodeLookupService, BarcodeLookupService>()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });

// Register backup service
builder.Services.AddScoped<IBackupService, BackupService>();

// Register settings service
builder.Services.AddScoped<ISettingsService, SettingsService>();

// Register database seeder service
builder.Services.AddScoped<DatabaseSeeder>();

// Register backup background service
builder.Services.AddHostedService<BackupBackgroundService>();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "YourVerySecretKeyThatIsAtLeast32CharactersLong!";
var issuer = jwtSettings["Issuer"] ?? "SnackboxApi";
var audience = jwtSettings["Audience"] ?? "SnackboxClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();

// Configure CORS for Blazor app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorApp", policy =>
    {
        policy.WithOrigins("https://localhost:7001", "http://localhost:5001")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Check database availability and auto-apply migrations if possible
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        // Try to connect with retry logic (same as CheckDatabaseExistsAsync)
        var canConnect = false;
        Exception? lastException = null;

        for (int i = 0; i < 5; i++)
        {
            try
            {
                canConnect = await dbContext.Database.CanConnectAsync();
                if (canConnect)
                {
                    logger.LogInformation("Database connection successful on attempt {Attempt}", i + 1);
                    break;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                logger.LogWarning("Database connection attempt {Attempt} failed: {Message}", i + 1, ex.Message);
                if (i < 4) await Task.Delay(1000);
            }
        }

        if (!canConnect)
        {
            logger.LogWarning("Database is not available after 5 attempts. Last error: {Error}. Please use /database-setup page to initialize the database.",
                lastException?.Message ?? "Unknown");
        }
        else
        {
            // Database is accessible - check if we can query tables
            try
            {
                var userCount = await dbContext.Users.CountAsync();
                logger.LogInformation("Database is initialized with {Count} users", userCount);

                // Check for pending migrations and apply them automatically
                var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations.Count());
                    await dbContext.Database.MigrateAsync();
                    logger.LogInformation("Migrations applied successfully");
                }
                else
                {
                    logger.LogInformation("Database is up to date - no pending migrations");
                }
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01") // undefined_table
            {
                logger.LogInformation("Database exists but is not initialized (no tables). Please use /database-setup page.");
            }
            catch (Exception ex) when (ex.Message.Contains("does not exist") || ex.Message.Contains("relation"))
            {
                logger.LogInformation("Database exists but is not initialized. Please use /database-setup page.");
            }
        }
    }
    catch (Npgsql.NpgsqlException ex) when (ex.Message.Contains("does not exist"))
    {
        logger.LogWarning("Database does not exist: {Message}. Please use /database-setup page to initialize the database.", ex.Message);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database startup check failed: {Message}", ex.Message);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Snackbox API v1");
        options.RoutePrefix = "swagger";
    });
}

// Add middleware to disable caching for all API responses
app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";
    await next();
});

app.UseHttpsRedirection();
app.UseCors("AllowBlazorApp");
app.UseAuthentication();
app.UseAuthorization();

// Map health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapControllers();

app.Run();

// Make the Program class accessible for integration tests
namespace Snackbox.Api
{
    public class Program { }
}
