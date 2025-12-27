# Snackbox Project - Claude Guidelines

## Project Overview

Snackbox is a modern full-stack application leveraging the .NET ecosystem with a Blazor MAUI Hybrid frontend and a .NET 10 backend orchestrated by .NET Aspire.

## Technology Stack

### Backend (.NET 10)
- **Runtime**: .NET 10
- **Orchestration**: .NET Aspire
  - Service orchestration and configuration
  - Built-in observability and health checks
  - Simplified local development and deployment
- **Database**: PostgreSQL with Entity Framework Core
- **API Pattern**: RESTful API with ASP.NET Core Web API
- **Authentication**: JWT/OAuth2 recommended

### Frontend (Blazor MAUI Hybrid)
- **Framework**: Blazor with .NET MAUI
- **Deployment Targets**:
  - Web (Blazor WebAssembly or Blazor Server)
  - Windows (WinUI)
  - macOS
  - iOS
  - Android
- **Components**: Shared Razor components across all platforms
- **State Management**: Built-in Blazor state or Fluxor for complex scenarios
- **Localization**: IStringLocalizer with English as default

### Testing Strategy
- **Unit Tests**: xUnit for backend logic
- **Component Tests**: bUnit for Blazor components
- **Integration Tests**: WebApplicationFactory for API testing
- **E2E Tests**: Playwright for end-to-end scenarios

## Architecture Principles

### Backend Architecture

```
Snackbox.Api/
├── Controllers/        # API endpoints
├── Services/          # Business logic
├── Data/             # EF Core DbContext and configurations
├── Models/           # Domain models
└── Extensions/       # Service registration extensions

Snackbox.AppHost/     # Aspire orchestration
├── Program.cs        # Service registration and configuration

Snackbox.ServiceDefaults/  # Shared Aspire configurations
```

### Frontend Architecture

```
Snackbox.Web/
├── Components/       # Reusable Blazor components
├── Pages/           # Page components with routing
├── Services/        # API clients and business logic
├── Resources/       # Localization resources
├── wwwroot/        # Static assets
└── MauiProgram.cs  # MAUI configuration
```

## Development Workflow

### Setting Up the Environment

1. **Prerequisites**:
   - .NET 10 SDK
   - Visual Studio 2024 or JetBrains Rider
   - Docker (for PostgreSQL)
   - .NET Aspire workload: `dotnet workload install aspire`
   - .NET MAUI workload: `dotnet workload install maui`

2. **Initial Setup**:
   ```bash
   # Clone repository
   git clone <repository-url>
   cd Snackbox-claude
   
   # Restore dependencies
   dotnet restore
   
   # Run Aspire AppHost
   dotnet run --project src/Snackbox.AppHost
   ```

3. **Database Setup**:
   - PostgreSQL will be automatically configured via Aspire
   - Migrations are applied on startup or manually via:
     ```bash
     dotnet ef database update --project src/Snackbox.Api
     ```

### Building and Running

```bash
# Run entire stack via Aspire
dotnet run --project src/Snackbox.AppHost

# Build specific project
dotnet build src/Snackbox.Web

# Run tests
dotnet test
```

## Code Standards

### C# Conventions
- **Naming**: PascalCase for public members, camelCase for private fields (with underscore prefix `_fieldName`)
- **Async**: Always use async/await for I/O operations
- **Nullability**: Enable nullable reference types
- **Documentation**: XML comments for public APIs

### File Organization
- One class per file
- File name matches class name
- Group related files in folders
- Keep files under 500 lines

### Error Handling
- Use Problem Details (RFC 7807) for API errors
- Log all exceptions with structured logging
- Return appropriate HTTP status codes
- Provide user-friendly error messages

## Blazor MAUI Hybrid Specifics

### Platform-Specific Code
```csharp
#if WINDOWS
// Windows-specific code
#elif ANDROID
// Android-specific code
#elif IOS
// iOS-specific code
#endif
```

### Web vs Native Considerations
- Use `IJSRuntime` carefully (may not work in all contexts)
- Abstract platform-specific features behind interfaces
- Test on multiple platforms regularly
- Consider offline scenarios for mobile

### Component Structure
```razor
@page "/example"
@using Snackbox.Web.Services
@inject IStringLocalizer<ExamplePage> Localizer
@inject IExampleService ExampleService

<h3>@Localizer["Title"]</h3>

@code {
    // Component logic
}
```

## Database Guidelines

### Entity Framework Core Setup
```csharp
// Use Npgsql for PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
```

### Naming Conventions
- **Tables**: lowercase_with_underscores (e.g., `user_accounts`)
- **Columns**: lowercase_with_underscores (e.g., `created_at`)
- **Relationships**: Configure explicitly in `OnModelCreating`

### Migrations
```bash
# Add migration
dotnet ef migrations add MigrationName --project src/Snackbox.Api

# Update database
dotnet ef database update --project src/Snackbox.Api

# Remove last migration
dotnet ef migrations remove --project src/Snackbox.Api
```

### Database Seeding

**Always implement seed data** to provide a sensible starting dataset for development and testing.

#### Using HasData in OnModelCreating
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Seed reference data
    modelBuilder.Entity<Category>().HasData(
        new Category { Id = 1, Name = "Electronics", CreatedAt = DateTime.UtcNow },
        new Category { Id = 2, Name = "Books", CreatedAt = DateTime.UtcNow },
        new Category { Id = 3, Name = "Clothing", CreatedAt = DateTime.UtcNow }
    );

    // Seed sample items
    modelBuilder.Entity<Item>().HasData(
        new Item { Id = 1, Name = "Laptop", CategoryId = 1, Price = 999.99m, CreatedAt = DateTime.UtcNow },
        new Item { Id = 2, Name = "Programming Book", CategoryId = 2, Price = 49.99m, CreatedAt = DateTime.UtcNow }
    );
}
```

#### Seeder Service for Complex Data
```csharp
public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    
    public DatabaseSeeder(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task SeedAsync()
    {
        // Only seed if database is empty
        if (await _context.Items.AnyAsync())
            return;
            
        // Create sample data
        var categories = new List<Category>
        {
            new() { Name = "Electronics" },
            new() { Name = "Books" }
        };
        
        await _context.Categories.AddRangeAsync(categories);
        await _context.SaveChangesAsync();
        
        var items = new List<Item>
        {
            new() { Name = "Laptop", CategoryId = categories[0].Id, Price = 999.99m },
            new() { Name = "Programming Book", CategoryId = categories[1].Id, Price = 49.99m }
        };
        
        await _context.Items.AddRangeAsync(items);
        await _context.SaveChangesAsync();
    }
}

// Register and call in Program.cs
builder.Services.AddScoped<DatabaseSeeder>();

// After app.Run():
using var scope = app.Services.CreateScope();
var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
await seeder.SeedAsync();
```

#### What to Include in Seed Data
- **Reference data**: Categories, statuses, types, roles
- **Sample users**: Test accounts for different roles (development only)
- **Representative entities**: Realistic examples of main business objects
- **Edge cases**: Data for testing boundary conditions
- **Localized content**: Sample data in multiple languages if applicable

## Localization Implementation

### Resource Files
- Store in `Resources/` folder
- Format: `PageName.{culture}.resx`
- Default: `PageName.resx` (English)

### Usage in Components
```csharp
@inject IStringLocalizer<PageName> Localizer

<h1>@Localizer["WelcomeMessage"]</h1>
```

### Supported Languages
- **Default**: English (en-US)
- Add additional languages as needed with corresponding .resx files

## Testing Guidelines

### Unit Tests (xUnit)
```csharp
public class ServiceTests
{
    [Fact]
    public async Task MethodName_Scenario_ExpectedBehavior()
    {
        // Arrange
        var service = new Service();
        
        // Act
        var result = await service.MethodAsync();
        
        // Assert
        Assert.NotNull(result);
    }
}
```

### Component Tests (bUnit)
```csharp
[Fact]
public void Component_RendersCorrectly()
{
    // Arrange
    using var ctx = new TestContext();
    
    // Act
    var cut = ctx.RenderComponent<MyComponent>();
    
    // Assert
    cut.MarkupMatches("<expected-html/>");
}
```

### Integration Tests
```csharp
public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    
    [Fact]
    public async Task GetEndpoint_ReturnsSuccess()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/endpoint");
        response.EnsureSuccessStatusCode();
    }
}
```

## Aspire Configuration

### AppHost Configuration
```csharp
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .AddDatabase("snackboxdb");

var apiService = builder.AddProject<Projects.Snackbox_Api>("api")
    .WithReference(postgres);

builder.AddProject<Projects.Snackbox_Web>("web")
    .WithReference(apiService);

builder.Build().Run();
```

### Service Defaults
- Health checks for all services
- OpenTelemetry for distributed tracing
- Resilience patterns (retry, circuit breaker)
- Service discovery

## Security Considerations

- **Input Validation**: Validate all user inputs
- **Authentication**: Implement proper authentication/authorization
- **Secrets Management**: Use user-secrets for development, Azure Key Vault for production
- **CORS**: Configure appropriately for API access
- **HTTPS**: Enforce HTTPS in production
- **SQL Injection**: Use parameterized queries (EF Core handles this)

## Performance Best Practices

- **Database**: Use async methods, add appropriate indexes
- **API**: Implement caching where appropriate
- **Blazor**: Use virtualization for large lists
- **Mobile**: Minimize bundle size, lazy load components
- **Aspire**: Leverage built-in health checks and telemetry

## Deployment

### Development
- Run via Aspire AppHost for integrated experience
- Aspire dashboard provides monitoring and logs

### Production
- Deploy API as container or App Service
- Deploy web app as static site or Blazor Server
- Mobile apps via app stores
- Use managed PostgreSQL service
- Configure Aspire for production orchestration

## Common Tasks

### Adding a New API Endpoint
1. Create controller method
2. Add service method if needed
3. Update shared contracts
4. Write unit tests
5. Write integration tests
6. Update API documentation

### Adding a New Blazor Page
1. Create `.razor` file in `Pages/`
2. Add `@page` directive with route
3. Add localization resources
4. Implement component logic
5. Write bUnit tests
6. Test on web and mobile

### Adding a New Database Entity
1. Create entity class
2. Add DbSet to DbContext
3. Configure in OnModelCreating
4. Add seed data using HasData or seeder service
5. Create migration
6. Update database
7. Add repository/service methods
8. Write tests

## Database Seeding Best Practices

### Key Principles
- Seed data is **automatically injected** when database is created
- Seed data should be **sensible and realistic**
- Include both **reference data** and **sample business data**
- Make seed data **appropriate for development and testing**
- Keep seed data **version-controlled** with migrations

### Implementation Approaches

**Static Data (HasData)**
- Use for reference data that rarely changes
- Use for lookup tables (categories, statuses, roles)
- Data is included in migrations

**Dynamic Data (Seeder Service)**
- Use for complex relationships
- Use for larger sample datasets
- Use when data needs conditional logic
- Run after migrations on application startup

### Seed Data Checklist
- [ ] Reference/lookup tables populated
- [ ] Sample users with different roles (dev/test only)
- [ ] Representative business entities
- [ ] Related entities with proper foreign keys
- [ ] Data covers common use cases
- [ ] Data includes edge cases for testing
- [ ] Localized content if applicable
- [ ] Timestamps set appropriately

## Resources and Documentation

- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [.NET MAUI Documentation](https://learn.microsoft.com/en-us/dotnet/maui/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [xUnit Documentation](https://xunit.net/)
- [bUnit Documentation](https://bunit.dev/)

## Support and Troubleshooting

### Common Issues

1. **Aspire not starting**: Ensure workload is installed
2. **Database connection fails**: Check PostgreSQL is running
3. **MAUI build errors**: Verify MAUI workload installation
4. **Localization not working**: Check resource file build action is `EmbeddedResource`

### Getting Help

- Check documentation links above
- Review existing tests for examples
- Consult team members
- Check GitHub issues for known problems
