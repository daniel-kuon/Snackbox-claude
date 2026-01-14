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

// Render/Container runtime: bind to PORT on 0.0.0.0 if provided
var portEnv = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(portEnv) && int.TryParse(portEnv, out var port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

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
// Prefer DATABASE_URL (e.g. provided by Render), otherwise fall back to ConnectionStrings:snackboxdb (e.g. provided by Aspire/AppHost)
var connectionString = builder.Configuration["DATABASE_URL"]
                      ?? builder.Configuration.GetConnectionString("snackboxdb")
                      ?? throw new InvalidOperationException("Database connection string is not configured (DATABASE_URL or ConnectionStrings:snackboxdb).");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

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
        var allowedOriginsEnv = builder.Configuration["ALLOWED_ORIGINS"]; // comma-separated list
        if (!string.IsNullOrWhiteSpace(allowedOriginsEnv))
        {
            var origins = allowedOriginsEnv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            policy.WithOrigins(origins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            // Development defaults
            policy.WithOrigins("https://localhost:7001", "http://localhost:5001")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

var app = builder.Build();

// Apply migrations and seed database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        await dbContext.Database.MigrateAsync();
    } catch
    {
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
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

// Avoid HTTPS redirection when running behind a platform proxy (e.g., when PORT is set)
if (string.IsNullOrWhiteSpace(portEnv))
{
    app.UseHttpsRedirection();
}
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
