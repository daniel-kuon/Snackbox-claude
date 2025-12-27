# Snackbox Project - GitHub Copilot Guidelines

## Project Quick Reference

**Tech Stack**: .NET 10 | Blazor MAUI Hybrid | .NET Aspire | PostgreSQL

## Project Structure

```
Snackbox-claude/
├── src/
│   ├── Snackbox.AppHost/          # Aspire orchestration entry point
│   ├── Snackbox.ServiceDefaults/  # Shared Aspire configurations
│   ├── Snackbox.Api/              # Backend API (.NET 10)
│   ├── Snackbox.Web/              # Blazor MAUI Hybrid frontend
│   └── Snackbox.Shared/           # Shared models and contracts
├── tests/
│   ├── Snackbox.Api.Tests/        # Backend unit tests
│   ├── Snackbox.Web.Tests/        # Frontend component tests
│   └── Snackbox.Integration.Tests/# Integration tests
└── docs/                           # Additional documentation
```

## Quick Commands

```bash
# Install required workloads
dotnet workload install aspire
dotnet workload install maui

# Restore and build
dotnet restore
dotnet build

# Run via Aspire (starts all services)
dotnet run --project src/Snackbox.AppHost

# Run tests
dotnet test

# Add migration
dotnet ef migrations add MigrationName --project src/Snackbox.Api

# Update database
dotnet ef database update --project src/Snackbox.Api
```

## Backend API Patterns

### Controller Template
```csharp
[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly IItemService _itemService;
    private readonly ILogger<ItemsController> _logger;

    public ItemsController(IItemService itemService, ILogger<ItemsController> logger)
    {
        _itemService = itemService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemDto>>> GetItems()
    {
        var items = await _itemService.GetAllAsync();
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ItemDto>> GetItem(int id)
    {
        var item = await _itemService.GetByIdAsync(id);
        if (item == null)
            return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ItemDto>> CreateItem(CreateItemDto dto)
    {
        var item = await _itemService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetItem), new { id = item.Id }, item);
    }
}
```

### Service Template
```csharp
public interface IItemService
{
    Task<IEnumerable<ItemDto>> GetAllAsync();
    Task<ItemDto?> GetByIdAsync(int id);
    Task<ItemDto> CreateAsync(CreateItemDto dto);
}

public class ItemService : IItemService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ItemService> _logger;

    public ItemService(ApplicationDbContext context, ILogger<ItemService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<ItemDto>> GetAllAsync()
    {
        return await _context.Items
            .Select(i => new ItemDto { Id = i.Id, Name = i.Name })
            .ToListAsync();
    }
}
```

### DbContext Configuration
```csharp
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Item> Items => Set<Item>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure PostgreSQL naming conventions
        modelBuilder.Entity<Item>(entity =>
        {
            entity.ToTable("items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        // Seed data - automatically injected when database is created
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Electronics", CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Name = "Books", CreatedAt = DateTime.UtcNow }
        );

        modelBuilder.Entity<Item>().HasData(
            new Item { Id = 1, Name = "Laptop", CategoryId = 1, Price = 999.99m, CreatedAt = DateTime.UtcNow },
            new Item { Id = 2, Name = "C# Book", CategoryId = 2, Price = 49.99m, CreatedAt = DateTime.UtcNow }
        );
    }
}
```

### Database Seeder Service
```csharp
// For complex seed data with relationships
public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(ApplicationDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (await _context.Items.AnyAsync())
        {
            _logger.LogInformation("Database already seeded");
            return;
        }

        _logger.LogInformation("Seeding database...");

        var categories = new List<Category>
        {
            new() { Name = "Electronics", CreatedAt = DateTime.UtcNow },
            new() { Name = "Books", CreatedAt = DateTime.UtcNow },
            new() { Name = "Clothing", CreatedAt = DateTime.UtcNow }
        };

        await _context.Categories.AddRangeAsync(categories);
        await _context.SaveChangesAsync();

        var items = new List<Item>
        {
            new() { Name = "Laptop", CategoryId = categories[0].Id, Price = 999.99m, CreatedAt = DateTime.UtcNow },
            new() { Name = "Mouse", CategoryId = categories[0].Id, Price = 29.99m, CreatedAt = DateTime.UtcNow },
            new() { Name = "C# Programming", CategoryId = categories[1].Id, Price = 49.99m, CreatedAt = DateTime.UtcNow }
        };

        await _context.Items.AddRangeAsync(items);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Database seeded successfully");
    }
}

// In Program.cs - register and call seeder
builder.Services.AddScoped<DatabaseSeeder>();

var app = builder.Build();

// Seed database on startup
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

app.Run();
```

## Frontend Blazor Patterns

### Page Component Template
```razor
@page "/items"
@using Snackbox.Web.Services
@inject IItemApiClient ItemApi
@inject IStringLocalizer<ItemsPage> Localizer
@inject NavigationManager Navigation

<PageTitle>@Localizer["Title"]</PageTitle>

<h1>@Localizer["Heading"]</h1>

@if (items == null)
{
    <p><em>@Localizer["Loading"]</em></p>
}
else
{
    <div class="item-grid">
        @foreach (var item in items)
        {
            <div class="item-card">
                <h3>@item.Name</h3>
                <button @onclick="() => ViewDetails(item.Id)">
                    @Localizer["ViewDetails"]
                </button>
            </div>
        }
    </div>
}

@code {
    private IEnumerable<ItemDto>? items;

    protected override async Task OnInitializedAsync()
    {
        items = await ItemApi.GetItemsAsync();
    }

    private void ViewDetails(int id)
    {
        Navigation.NavigateTo($"/items/{id}");
    }
}
```

### Reusable Component Template
```razor
@* Components/ItemCard.razor *@
@inject IStringLocalizer<ItemCard> Localizer

<div class="item-card">
    <h3>@Item.Name</h3>
    <p>@Item.Description</p>
    <button @onclick="OnViewClicked">@Localizer["View"]</button>
</div>

@code {
    [Parameter, EditorRequired]
    public ItemDto Item { get; set; } = default!;

    [Parameter]
    public EventCallback<int> OnView { get; set; }

    private async Task OnViewClicked()
    {
        if (OnView.HasDelegate)
            await OnView.InvokeAsync(Item.Id);
    }
}
```

### API Client Service
```csharp
public interface IItemApiClient
{
    Task<IEnumerable<ItemDto>> GetItemsAsync();
    Task<ItemDto?> GetItemAsync(int id);
    Task<ItemDto> CreateItemAsync(CreateItemDto dto);
}

public class ItemApiClient : IItemApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ItemApiClient> _logger;

    public ItemApiClient(HttpClient httpClient, ILogger<ItemApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<ItemDto>> GetItemsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<IEnumerable<ItemDto>>("api/items")
                ?? Enumerable.Empty<ItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching items");
            throw;
        }
    }
}
```

## Aspire Configuration Patterns

### AppHost Program.cs
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("snackboxdb");

// Add backend API
var apiService = builder.AddProject<Projects.Snackbox_Api>("api")
    .WithReference(postgres)
    .WithExternalHttpEndpoints();

// Add Blazor MAUI Hybrid web
builder.AddProject<Projects.Snackbox_Web>("web")
    .WithReference(apiService)
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

### ServiceDefaults Extensions
```csharp
public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.Services.AddServiceDiscovery();
        
        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation())
            .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation());

        return builder;
    }
}
```

## Testing Patterns

### xUnit Unit Test
```csharp
public class ItemServiceTests
{
    private readonly Mock<ApplicationDbContext> _mockContext;
    private readonly Mock<ILogger<ItemService>> _mockLogger;
    private readonly ItemService _service;

    public ItemServiceTests()
    {
        _mockContext = new Mock<ApplicationDbContext>();
        _mockLogger = new Mock<ILogger<ItemService>>();
        _service = new ItemService(_mockContext.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsItems()
    {
        // Arrange
        var items = new List<Item>
        {
            new() { Id = 1, Name = "Test Item" }
        }.AsQueryable();
        
        _mockContext.Setup(c => c.Items).Returns(MockDbSet(items));

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Single(result);
    }
}
```

### bUnit Component Test
```csharp
public class ItemCardTests : TestContext
{
    [Fact]
    public void ItemCard_RendersItemName()
    {
        // Arrange
        var item = new ItemDto { Id = 1, Name = "Test Item" };
        Services.AddSingleton<IStringLocalizer<ItemCard>>(new MockStringLocalizer<ItemCard>());

        // Act
        var cut = RenderComponent<ItemCard>(parameters => parameters
            .Add(p => p.Item, item));

        // Assert
        cut.Find("h3").MarkupMatches("<h3>Test Item</h3>");
    }
}
```

### Integration Test
```csharp
public class ItemsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ItemsApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetItems_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/items");

        // Assert
        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<IEnumerable<ItemDto>>();
        Assert.NotNull(items);
    }
}
```

## Localization Patterns

### Resource File Structure
```
Resources/
├── Pages/
│   ├── ItemsPage.resx          # Default (English)
│   ├── ItemsPage.es.resx       # Spanish
│   └── ItemsPage.fr.resx       # French
└── Components/
    ├── ItemCard.resx
    └── ItemCard.es.resx
```

### Using Localization in Code
```csharp
// In Razor component
@inject IStringLocalizer<ItemsPage> Localizer
<h1>@Localizer["WelcomeMessage"]</h1>

// In C# class
public class ItemService
{
    private readonly IStringLocalizer<ItemService> _localizer;
    
    public ItemService(IStringLocalizer<ItemService> localizer)
    {
        _localizer = localizer;
    }
    
    public string GetErrorMessage()
    {
        return _localizer["ErrorOccurred"];
    }
}
```

### MauiProgram.cs Setup
```csharp
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Add Blazor WebView
        builder.Services.AddMauiBlazorWebView();

        #if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        #endif

        // Add localization
        builder.Services.AddLocalization();

        // Add API client
        builder.Services.AddHttpClient<IItemApiClient, ItemApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://localhost:7001"); // From Aspire
        });

        return builder.Build();
    }
}
```

## Common Code Snippets

### Entity Base Class
```csharp
public abstract class EntityBase
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
```

### API Response Wrapper
```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Errors { get; set; } = new();
}
```

### Loading State Component
```razor
@if (IsLoading)
{
    <div class="loading-spinner">
        <span>@Localizer["Loading"]</span>
    </div>
}
else if (Error != null)
{
    <div class="error-message">
        <p>@Localizer["Error"]: @Error</p>
    </div>
}
else
{
    @ChildContent
}

@code {
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public string? Error { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Inject] private IStringLocalizer<LoadingWrapper> Localizer { get; set; } = default!;
}
```

## Naming Conventions

### C# Code
- **Classes/Interfaces**: PascalCase (e.g., `ItemService`, `IItemRepository`)
- **Methods**: PascalCase (e.g., `GetItemAsync`)
- **Properties**: PascalCase (e.g., `ItemName`)
- **Private fields**: camelCase with underscore (e.g., `_itemService`)
- **Parameters**: camelCase (e.g., `itemId`)
- **Async methods**: Suffix with `Async`

### Database (PostgreSQL)
- **Tables**: lowercase_with_underscores (e.g., `user_items`)
- **Columns**: lowercase_with_underscores (e.g., `created_at`)
- **Indexes**: `ix_tablename_columnname`
- **Foreign keys**: `fk_tablename_referencedtable`

### Files
- **Components**: PascalCase (e.g., `ItemCard.razor`)
- **Pages**: PascalCase (e.g., `ItemsPage.razor`)
- **Services**: PascalCase (e.g., `ItemService.cs`)
- **Tests**: PascalCase with Tests suffix (e.g., `ItemServiceTests.cs`)

## Key Dependencies

### Backend (Snackbox.Api.csproj)
```xml
<ItemGroup>
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.*" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.*" />
  <PackageReference Include="Aspire.Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.*" />
</ItemGroup>
```

### Frontend (Snackbox.Web.csproj)
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Maui.Controls" Version="9.0.*" />
  <PackageReference Include="Microsoft.AspNetCore.Components.WebView.Maui" Version="9.0.*" />
  <PackageReference Include="Microsoft.Extensions.Localization" Version="9.0.*" />
</ItemGroup>
```

### AppHost (Snackbox.AppHost.csproj)
```xml
<ItemGroup>
  <PackageReference Include="Aspire.Hosting.AppHost" Version="9.0.*" />
  <PackageReference Include="Aspire.Hosting.PostgreSQL" Version="9.0.*" />
</ItemGroup>
```

## Performance Tips

1. **Use async/await** for all I/O operations
2. **Add database indexes** for frequently queried columns
3. **Use `.AsNoTracking()`** for read-only queries
4. **Implement pagination** for large data sets
5. **Use response compression** in API
6. **Lazy load Blazor components** where appropriate
7. **Cache static data** with `IMemoryCache`
8. **Use `@key`** directive for list items in Blazor

## Security Checklist

- [ ] Enable HTTPS in production
- [ ] Validate all user inputs
- [ ] Use parameterized queries (EF Core does this)
- [ ] Implement authentication/authorization
- [ ] Store secrets in user-secrets or Key Vault
- [ ] Configure CORS appropriately
- [ ] Enable request rate limiting
- [ ] Sanitize error messages (don't leak sensitive info)
- [ ] Use connection string encryption
- [ ] Implement proper logging (avoid logging sensitive data)

## Troubleshooting

### Common Issues

**Aspire Dashboard not loading**
- Ensure `dotnet workload install aspire` was run
- Check port 15000 (default dashboard port) is available

**Database connection fails**
- Verify PostgreSQL is running (check Aspire dashboard)
- Check connection string in appsettings.json

**MAUI build errors**
- Run `dotnet workload install maui`
- Clean and rebuild solution
- Check for platform-specific issues

**Localization not working**
- Verify resource files have `EmbeddedResource` build action
- Check culture is set correctly
- Ensure `AddLocalization()` is called in startup

## Additional Resources

- [.NET Aspire Docs](https://learn.microsoft.com/dotnet/aspire)
- [Blazor Docs](https://learn.microsoft.com/aspnet/core/blazor)
- [MAUI Docs](https://learn.microsoft.com/dotnet/maui)
- [EF Core with PostgreSQL](https://www.npgsql.org/efcore)
- [xUnit Docs](https://xunit.net)
- [bUnit Docs](https://bunit.dev)
