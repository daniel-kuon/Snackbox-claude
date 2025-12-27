# Snackbox Project - Junie Guidelines

## Project Overview

Snackbox is a modern full-stack application built with .NET technologies, featuring a Blazor MAUI Hybrid frontend and a .NET 10 backend with .NET Aspire for orchestration.

## Technology Stack

### Backend
- **Framework**: .NET 10
- **Orchestration**: .NET Aspire for development and deployment
- **Database**: PostgreSQL
- **Architecture**: Clean Architecture / Vertical Slice Architecture recommended

### Frontend
- **Framework**: Blazor MAUI Hybrid App
- **Web Support**: Must be runnable as a web application
- **Mobile Support**: Native mobile app support via MAUI
- **Localization**: English as default language with full localization support
- **UI Framework**: Consider FluentUI Blazor or MudBlazor for components

### Testing
- **Backend Tests**: xUnit or NUnit
- **Frontend Tests**: bUnit for Blazor components
- **Integration Tests**: Required for API endpoints
- **E2E Tests**: Playwright for cross-platform testing

## Project Structure

```
/
├── src/
│   ├── Snackbox.Web/              # Blazor MAUI Hybrid app
│   ├── Snackbox.Api/              # Backend API
│   ├── Snackbox.AppHost/          # Aspire AppHost
│   ├── Snackbox.ServiceDefaults/  # Aspire service defaults
│   └── Snackbox.Shared/           # Shared models and contracts
├── tests/
│   ├── Snackbox.Web.Tests/
│   ├── Snackbox.Api.Tests/
│   └── Snackbox.Integration.Tests/
└── docs/
```

## Development Guidelines

### When Working on Backend Tasks

1. **Use .NET Aspire** for service orchestration and configuration
2. **Follow REST API conventions** for endpoints
3. **Implement proper error handling** with Problem Details
4. **Use Entity Framework Core** for database access
5. **Write unit tests** for business logic
6. **Write integration tests** for API endpoints
7. **Use dependency injection** throughout
8. **Implement logging** using ILogger

### When Working on Frontend Tasks

1. **Use Blazor components** with proper lifecycle management
2. **Implement localization** using IStringLocalizer
3. **Ensure web and mobile compatibility**
4. **Use responsive design** patterns
5. **Write bUnit tests** for components
6. **Handle loading and error states** appropriately
7. **Use proper data binding** and validation

### Database

1. **Use EF Core migrations** for schema changes
2. **Follow PostgreSQL naming conventions** (lowercase, underscores)
3. **Create indexes** for frequently queried columns
4. **Use repositories or direct DbContext** based on complexity

### Localization

1. **Default language**: English (en-US)
2. **Store resources** in .resx files or JSON
3. **Use IStringLocalizer** for all user-facing text
4. **Test with multiple languages** before merging

## Aspire Integration

.NET Aspire provides:
- **Service discovery** between components
- **Health checks** for dependencies
- **Dashboard** for monitoring during development
- **Configuration management** across services
- **Telemetry** and logging infrastructure

## Testing Requirements

- **Minimum code coverage**: 70%
- **All public APIs must have tests**
- **Critical business logic requires tests**
- **Test edge cases and error scenarios**

## Code Quality

- **Follow C# conventions** (PascalCase for public members, camelCase for private)
- **Use nullable reference types**
- **Enable all warnings as errors** in production code
- **Run code analysis** before committing
- **Use async/await properly**

## Getting Started

1. Install .NET 10 SDK
2. Install .NET Aspire workload: `dotnet workload install aspire`
3. Install PostgreSQL or use Docker
4. For mobile development, install MAUI workload: `dotnet workload install maui`
5. Run the Aspire AppHost to start all services
6. Access the web app through the Aspire dashboard

## Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Blazor MAUI Hybrid](https://learn.microsoft.com/en-us/aspnet/core/blazor/hybrid/)
- [PostgreSQL with EF Core](https://www.npgsql.org/efcore/)
- [Localization in Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor/globalization-localization)