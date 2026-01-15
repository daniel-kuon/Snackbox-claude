# Snackbox Project - Junie Guidelines

## What is Snackbox?

**IMPORTANT: Keep this description up-to-date as the project evolves. Update it when features are added, modified, or removed.**

Snackbox is an employee snack purchasing system that enables staff to buy snacks by scanning barcodes. The application provides:

### Core Features
- **Employee Self-Service**: Users scan product barcodes to purchase snacks
- **Financial Tracking**: 
  - Tracks how much each employee has spent
  - Tracks how much each employee has paid into the system
  - Shows current debt/credit balance for each user
  - Displays purchase history
- **User Roles**:
  - Regular users can scan, purchase, and view their own financial status
  - Admin users can enter payments and manage stock
- **Stock Management**:
  - Tracks quantities in storage
  - Tracks quantities on the shelf (manually updated by admins)
  - Does NOT automatically reduce stock quantities when purchases are made
  - Admins manually update shelf quantities based on visual inspection
- **Product Batch Management**:
  - Supports products with multiple batches
  - Each batch has its own "best before" date
  - Enables proper inventory rotation and expiry management

## Technical Overview

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
5. **Implement database seeding** with sensible seed data that is automatically injected when the database is created

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

## Database Seeding

- **Always implement seed data** for development and testing
- **Create meaningful, realistic sample data** that represents production scenarios
- **Seed data should be injected automatically** when the database is created or updated
- **Use EF Core's `HasData()` method** in `OnModelCreating` for static seed data
- **Implement a seeder service** for complex or dynamic seed data
- **Include seed data for**:
  - Reference data (categories, types, statuses)
  - Sample users and roles (for development/testing)
  - Representative business entities
  - Test data for common scenarios

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

---

## Render Deployment

Hinweis: Dieses Repo ist für Deployment auf Render vorbereitet, ohne Aspire-basierte lokale Entwicklung zu beschädigen.

Deploybare Projekte:
- API: src/Snackbox.Api (ASP.NET Core Web API)
- Web: src/Snackbox.BlazorServer (Blazor Server Frontend)

Nicht deployen (explizit ignorieren):
- Aspire AppHost (src/Snackbox.AppHost)
- Native MAUI/Blazor Hybrid App (src/Snackbox.Web)

Benötigte Environment Variables auf Render:
- Snackbox.Api (Web Service "snackbox-api"):
  - DATABASE_URL (wird automatisch von der Render-Postgres-DB gesetzt/verlinkt)
  - ALLOWED_ORIGINS (z. B. https://<dein-web-service>.onrender.com – wird im Blueprint vom Web-Service referenziert)
  - ASPNETCORE_ENVIRONMENT = Production (im Blueprint gesetzt)
  - JwtSettings__SecretKey, JwtSettings__Issuer, JwtSettings__Audience (im Dashboard setzen, falls Auth aktiv)
  - BarcodeLookup__ApiKey (falls BarcodeLookup genutzt wird)
  - EmailSettings__… (falls E-Mail-Versand genutzt wird)
- Snackbox.BlazorServer (Web Service "snackbox-web"):
  - API_URL (wird im Blueprint automatisch auf die URL des API-Services gesetzt)

Runtime-Anpassungen (provider-neutral):
- Beide Apps binden bei gesetzter PORT-Variable auf http://0.0.0.0:${PORT} (kein HTTPS-Binding).
- API liest PostgreSQL-Connection aus DATABASE_URL; Fallback auf Aspire-ConnectionStrings:snackboxdb.
- CORS: ALLOWED_ORIGINS (kommagetrennt). Ohne EnvVar gelten Dev-Defaults (localhost-Origins).

Lokale Entwicklung mit Aspire bleibt funktionsfähig:
- AppHost provisioniert PostgreSQL und setzt ConnectionStrings:snackboxdb; Code fällt korrekt zurück, wenn keine DATABASE_URL gesetzt ist.
- Dev-Origins für CORS bleiben bestehen.

Blueprint-Datei:
- render.yaml im Repository-Wurzelverzeichnis definiert eine Postgres-Datenbank sowie zwei Web Services (API und Blazor Server) und führt dotnet publish im Build aus.