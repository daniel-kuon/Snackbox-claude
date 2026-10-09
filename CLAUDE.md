# Snackbox Project - Claude Guidelines

## What is Snackbox?

**IMPORTANT: This section describes the business domain and requirements. Keep it up-to-date as features are implemented, modified, or removed. When working on new features, refer to this description to ensure alignment with the project's purpose.**

Snackbox is an employee snack purchasing and inventory management system that streamlines the process of buying snacks in the workplace.

### Business Domain

**Purpose**: Enable employees to purchase snacks through a self-service barcode scanning system while maintaining accurate financial tracking and inventory management.

**Key Stakeholders**:
- **Employees (Regular Users)**: Purchase snacks and track their spending
- **Administrators**: Manage inventory, enter payments, and oversee the system

### Core Functionality

#### 1. Purchase System
- Employees scan product barcodes to make purchases
- System records each transaction
- Purchases are tracked against the employee's account

- **Unknown codes**: a scan of a code in no table shows a large centred error with the code (click to dismiss, gone after 5 s) and is stored in `unknown_scans`. The admin dashboard's *Last scans* lists the newest kiosk scans, found and not found (`GET /api/scanner/recent?count=`, admin only)

#### 2. Financial Management
- **Spending Tracking**: Records how much each employee has spent
- **Payment Tracking**: Records how much each employee has paid into the system
- **Balance Calculation**: Shows current debt (spent more than paid) or credit (paid more than spent)
- **Purchase History**: Users can view their past purchases and amounts
- **Payment Entry**: Admins can record when employees make payments

#### 3. Account Setup and Onboarding
- **New Cards Work Immediately**: A card assigned to a not-yet-set-up (inactive placeholder) user can make purchases right away; a banner on the scan screen points this out
- **Card wizard** (Admin -> Users -> *Add cards*, `/admin/cards`): pre-made cards are numbered and each carries a EUR 0.30 and a EUR 0.50 code. Enter the first number and how many cards; the page lists them and activates the first card not yet set up. Scan its 0.30 code, then its 0.50 code (the scanner types into a focused field and ends with Enter, so it works on the kiosk and the website alike); the card is saved and the next higher card that is not set up yet becomes active. Clicking a card starts its two scans over. `GET /api/cards?from=&count=`, `PUT /api/cards/{number}` (admin). A new card is an inactive user `Karte NN` with `User.CardNumber` set - the number, not the name, identifies the card, because claiming renames the user. A re-scan replaces the codes in place (matching by code first, so correcting swapped codes moves amounts rather than codes and keeps purchase history); codes belonging to another user are refused. The legacy import and a migration backfill `CardNumber` for existing `Karte NN` users
- **Self-Service Setup Wizard**: When an inactive user's card is scanned on the kiosk, a wizard opens where they enter their name and e-mail (both required - the e-mail is how the admin reaches them about their balance) to activate their account. The scan screen's "set up your account" banner is hidden while that wizard is open — no admin needed (the scanned barcode acts as proof of card possession)
- **First-Time Introduction (spotlight tour)**: The wizard also runs a short introduction that highlights the relevant part of the scan screen for each step (buying, paying, history/achievements/discounts, and — when enabled — the phone app). It is shown once to every user on their first scan. Steps are tracked **per step** in the `user_wizard_steps` table (not a single flag), so a step added later for a newly enabled feature is shown to existing users without replaying the whole intro. The server's `WizardStepCatalog` is the single source of the steps and returns each scan's `PendingWizardSteps` (already filtered by enabled features)
- **Idle auto-advance**: On the kiosk the tour plays itself when the mouse is idle; per-step duration scales with the amount of text. The first mouse movement hands control to the user (manual Next/Back)
- **Spotlight on two elements**: a step can light up more than one element - *paying* highlights the PayPal QR (`sb-spotlight`, whose huge box-shadow is the dimming layer) and the open amount (`sb-spotlight-extra`, just the ring, one z-index higher so the first element's shadow does not cover it)
- **Mouse and Keyboard**: The wizard supports mouse and keyboard (Enter/arrow keys/Esc)
- **Feature flags**: Admin-toggleable switches live in the `feature_flags` table and take effect immediately (no restart). `GET /api/featureflags` is anonymous so the kiosk can honor them; `PUT /api/featureflags/{key}` is admin-only and exposed in Admin → Settings → Features. The `mobile_app` flag gates the phone-app QR on the scan screen and its wizard step
- **Phone App (PWA)**: The Blazor Server website is installable on phones as a PWA (manifest + service worker), gated by the `mobile_app` feature flag. It is only reachable in the local Wi-Fi via the PC's IP with a self-signed certificate. `GET /install` (plain HTTP, exempt from HTTPS redirect) serves a step-by-step guide and `GET /install/certificate` downloads the public certificate (read from `Kestrel:Certificates:Default:Path`/`Password`). The kiosk shows the website URL from `Website:PublicUrl` config or falls back to the PC's IP
- **Login without a card**: the machine is maintained remotely, where nobody can scan. The kiosk's welcome screen has an *Admin* link to `/login?returnUrl=/scan` (e-mail + password); the login page shows *Cancel* back to the scan screen for any local `returnUrl` (absolute and protocol-relative URLs are ignored)
- **First administrator**: creating an empty database (`/database-setup`, `POST /api/backup/database/create-empty`) requires an `InitialAdminDto` (name, e-mail, password >= 6, the same rule as register/change password) and creates that admin right after the migrations - an empty installation would otherwise have nobody who can log in. The endpoint is anonymous only while no database exists
- **Dev test helper**: In Development with `TestHelper:Enabled`, `api/testhelper/*` powers a kiosk overlay to simulate scans by click, bypass the login password, and reset+reseed the database

#### 4. Kiosk Window Modes (parallel run with the old Snackbox)
- **Default**: the kiosk window is fullscreen (`Window:StartFullscreen`). It drops out of fullscreen when it loses focus and puts itself back to fullscreen **and to the foreground** on the next scan (`Window:AutoFocusOnScan`). Getting the foreground back from another app needs `AttachThreadInput` - a plain `SetForegroundWindow` is refused by Windows and leaves the kiosk fullscreen-sized but behind the other window
- **Not while an admin is logged in on the kiosk**: no fullscreen/foreground on scans then - the admin is working in the window (remotely, or scanning cards into the card wizard)
- **Admin -> Settings -> Kiosk window** (Foreground / Background) switches the parallel-run mode below without touching the machine. Stored as the `kiosk_background` row of `feature_flags` (on = `Everyone`), shown as its own switch rather than in the Features list; the kiosk polls `GET /api/featureflags` every 30 s and minimizes itself or comes back to fullscreen. `Window:StartMinimized` is only the starting value until the API answers
- **`Window:StartMinimized`**: parallel-run mode for the first weeks alongside the old Snackbox. The kiosk starts minimized and stays there (it re-minimizes once after MAUI's initial activation); it never goes fullscreen and never pulls focus on a scan. Overrides `StartFullscreen`/`AutoFocusOnScan`
- **Both apps see every scan in either mode**: the kiosk reads the scanner through a global `WH_KEYBOARD_LL` hook (`WindowsScannerListener`) that passes keystrokes on via `CallNextHookEx`, and the old Snackbox runs its own global hook (`GlobalKey=True` in its `option.ini`). Window focus is irrelevant for both. Keep the hook callback fast - window work is dispatched off the hook thread, because Windows evicts a slow low-level hook and that would cost scans
- **Flipping the setting without a rebuild**: besides the embedded `Resources/Raw/appsettings.json`, the kiosk also reads an optional `appsettings.json` next to `Snackbox.Web.exe`, which wins

#### 5. User Roles and Permissions
- **Regular Users**:
  - Scan barcodes to purchase snacks
  - View their own purchase history
  - View their current balance (debt/credit)
  - Make purchases that are recorded against their account
- **Admin Users**:
  - All regular user capabilities
  - Enter payment transactions for employees
  - Manage product inventory
  - Update stock quantities
  - Add/edit/remove products
  - Create and manage discounts

#### 6. Stock Management
- **Two-Tier Inventory**:
  - **Storage**: Products kept in reserve/storage area
  - **Shelf**: Products currently available for purchase
- **Manual Stock Updates**: Admins manually update shelf quantities (system does not auto-decrement based on purchases)
- **Stock Tracking**: System tracks quantities in both storage and on shelf
- **Admin Workflow**: When restocking, admin moves quantity from storage to shelf

#### 7. Product and Batch Management
- **Products**: Individual snack items with barcodes
- **Multiple Batches**: Each product can have multiple batches
- **Best Before Dates**: Each batch has its own expiration date
- **Batch Tracking**: Enables:
  - First-in-first-out (FIFO) inventory rotation
  - Expiry management and alerts
  - Batch-level stock tracking
  - Removal of expired batches

#### 8. Achievement System
- **Gamification**: Fun achievements awarded based on purchasing behavior
- **Categories**: Single purchase, daily activity, streaks, comebacks, debt levels, total spending
- **Automatic Awards**: Achievements earned automatically when criteria met
- **Visual Notifications**: Animated overlays with celebration effects
- **Humor**: Lighthearted names and descriptions (e.g., "Living on the Edge" for high debt)
- **One-Time Only**: Each achievement can only be earned once per user
- See [Achievement System Documentation](docs/achievement-system.md) for full details

#### 9. Discount System
- **Automatic Application**: Discounts are automatically detected and applied during barcode scanning
- **Two Types**: Fixed amount (e.g., 0.50€ off) or percentage (e.g., 10% off)
- **Date-Based Validity**: Discounts only active within their date range
- **Minimum Purchase**: Discounts can require a minimum purchase amount
- **Admin Management**: Only admins can create, update, or delete discounts
- **Best Discount Applied**: System automatically selects the discount providing highest savings
- See [Discount System Documentation](docs/discount-system.md) for full details

#### 10. Import from the old Snackbox
- **Admin -> Old Snackbox** (`/admin/legacy-import`) connects to the previous Snackbox's SQL Server by server/database/login (empty login = the API's Windows account) and pulls `T_User`, `T_UserCodes`, `T_Posten` and `T_ToPay` across; the time tracking tables are ignored. Endpoints: `POST /api/legacyimport/test|import|verify`, admin only, and excluded from the verbose HTTP body logging because the request carries that database's password
- **Grouping**: `T_Posten` is one row per barcode scan with no notion of a purchase, so the import rebuilds purchases with the same `Scanner:TimeoutSeconds` window the live scanner uses (`PurchaseGrouping.Group`, gap measured against the *previous* scan). Imported purchases get `IsLegacyImport = true`
- **Idempotent**: every imported row keeps the old primary key (`User.LegacyUserId`, `Barcode.LegacyCodeId`, `BarcodeScan.LegacyPostenId`, `Payment.LegacyToPayId`, each uniquely indexed where set). Re-running skips what is already here and can extend the newest imported purchase if a scan still falls inside its window, so the import can be repeated during the parallel-run period. `From` limits it to a date range, `DryRun` reports without writing
- **Balance anchor**: the old `T_User.rest` (a debt) does not equal its own scans minus payments - the history was trimmed over the years. `rest` is what people actually owe, so the import adds one correcting payment per user, marked by a fixed note and *replaced* rather than stacked on a re-run. Verification leaves those payments out of the "paid" comparison
- **Quirks it handles**: old codes deleted years ago leave ~3900 scans pointing at nothing (they go onto a per-user, non-scannable `LEGACY-IMPORT-<id>` barcode so the money is not lost), a few codes are shared by several users, several users share one e-mail (dropped, since e-mail is unique here), and `Karte N` users are imported as inactive spare cards
- **Verification** compares both databases scan by scan - matching by user, amount and timestamp within a tolerance, because scans the new app captured live have no legacy id. It reports per-user counts, spent/paid totals, old `rest` against the new balance, and lists the individual scans only one side has. This is the tool for confirming the new app missed nothing while both ran in parallel

#### 11. Installation, autostart and updates
- **An installation is a git checkout** that is built and run in place (`install-snackbox.ps1` clones, checks out the newest `v*.*.*` tag, builds Release, registers autostart and starts). The machine needs git, the .NET 10 SDK and Docker Desktop. The installed version *is* the checked-out tag - there is no version file to keep in sync
- **Why not a binary release**: the Aspire AppHost cannot be published and moved. Its generated `ProjectMetadata` hardcodes the build machine's absolute `.csproj` path, so a published AppHost looks for projects that do not exist on the target machine
- **`tools/Snackbox.Updater`** owns everything that has to happen outside the app: `status`, `update --tag <tag> [--no-start]`, `start [--no-kiosk]`, `stop`, `install-autostart`, `uninstall-autostart`. It finds its installation from `--dir`, then `SNACKBOX_HOME`, then by walking up to a directory with `Snackbox.sln` next to a `.git` entry (a worktree's `.git` is a *file*, so both are accepted)
- **Update flow**: fetch first (a network problem should cost no downtime), refuse if the checkout is dirty, stop the stack, check the tag out, `dotnet build Snackbox.sln -c Release`, start again. A failed build checks the previous commit back out, rebuilds and restarts, so a bad release cannot leave the machine dead. The updater copies itself to the temp folder and continues from there, because the build it runs would otherwise be unable to replace its own locked executable
- **Installations run as Production**: the updater starts the AppHost with the `Installed` launch profile, and the AppHost hands its own environment to the API and the website (`WithEnvironment("ASPNETCORE_ENVIRONMENT", ...)`). Without that both took Development from their own `launchSettings.json` - and in Development the API's test helper is live, i.e. a login without password and a database reset on the kiosk for anyone to click. From the IDE the default profile still runs everything as Development
- **Launching the update from the app**: the API must not start the updater as its own child. Everything Aspire runs lives in a Windows job object owned by DCP, and the first thing the updater does is stop that stack - as a child it would die with it, mid-update (the log then simply ends at "Fetching from origin..."). The API writes `<install>/.snackbox/update.cmd` and starts it through `explorer.exe`, which runs it from the user's shell: outside the job, on the visible desktop, in a terminal window that shows the progress and stays open afterwards (`--keep-window`: 60 s on success, until Enter on failure). The release tag is validated before it goes into that script
- **The AppHost writes to `.snackbox/apphost.log` via `cmd /c ... >`**, never into a pipe owned by the updater: the updater exits right after starting it, and the AppHost's next log line would hit a closed pipe
- **Stopping also kills our Aspire orchestrator** (`dcp.exe` started with `--monitor <AppHost pid>`, matched by command line so another Aspire app's DCP survives). Left alone it keeps tearing down its resources after the AppHost is gone - long enough to take the *next* AppHost's API and website down, which left an installation without a backend after an update
- **Stopping means all four processes**, not just the AppHost: Aspire's DCP starts `Snackbox.Api` and `Snackbox.BlazorServer` *outside* the AppHost's process tree, so killing the remembered `dotnet run` leaves them holding the DLLs the rebuild has to replace. `Stack.StackProcessNames` lists what gets killed - note the old Snackbox is `Snackboxx` with two x and is deliberately not in it
- **The updater's state lives in `<install>/.snackbox/`** and that folder ignores itself with a `.gitignore` of `*`. Without it the log and pid file make the checkout dirty, and the dirty check refuses every update the updater is there to perform - including on installations whose checked-out `.gitignore` predates the fix
- **Admin -> Updates** (`/admin/updates`) shows the installed version, the newest GitHub release and its notes, and an Install button. `GET /api/updates/status|log`, `POST /api/updates/install`, admin only. Install returns immediately and the page then polls `log` - the update stops the very API that served the request, so the updater's log file (`<install>/.snackbox/updater.log`) is where the outcome shows up. `Update:Repository`, `Update:IncludePreReleases`, `Update:InstallRoot` and `Update:GitHubToken` configure it
- **Autostart** runs at logon (not a service: Docker Desktop and the kiosk window both need a desktop session) and runs `Snackbox.Updater start`, which waits for Docker to answer `docker info`, starts the AppHost and then the kiosk. A scheduled task is preferred, but creating a logon-triggered one needs elevation, so without it the updater writes `Snackbox.cmd` into the Startup folder instead - the installer therefore does not require an administrator shell
- **Where the data lives - all of it outside the checkout, under fixed names**, so a reinstall to another folder, a moved folder or an Aspire upgrade cannot quietly start on an empty database:
  - Postgres 18: Docker volume `snackbox-postgres` mounted at `/var/lib/postgresql` (the 18 image keeps its data in a per-version folder below it, `18/docker`), container `snackbox-postgres`. The AppHost pins the volume name, the mount path **and the major version** (`WithImageTag("18")`), because Aspire's defaults keep moving: the default volume name is a hash of the AppHost's path, and the default image changes with Aspire (13.6 went from 17 to 18) - a major version cannot open another's data files, the container just exits. A major upgrade is a backup and restore (Admin -> Backups), never a tag bump; that is how 17 -> 18 was done
  - Backups run `pg_dump`/`psql` **inside that container** (`docker exec`), so the tools always match the server: `pg_dump` refuses to dump a newer server, which broke host-installed tools on every upgrade. No PostgreSQL install is needed on the machine. A restore stops at the first SQL error (`ON_ERROR_STOP`) and fails loudly - it used to log a warning and report success
  - Backups: `%ProgramData%\Snackbox\backups` (`Backup:Directory`; a relative value resolves against `%ProgramData%\Snackbox`, not the checkout)
  - SigNoz: the compose file names its volumes itself (`signoz-clickhouse`, `signoz-sqlite`, `signoz-zookeeper-1`)
- **Aspire** is 13.6 with `AspireUseCliBundle=false`: installations run the AppHost with plain `dotnet run` and have no Aspire CLI, so the orchestrator comes from the NuGet packages
- **Releases** are a tag plus notes, nothing more. `.github/workflows/release.yml` builds the solution with the same command the installation uses - if that fails, the release would brick every machine that installs it

#### 12. Observability (SigNoz)
- **Purpose**: Remote bug tracking. All three apps (API `snackbox-api`, phone website `snackbox-blazor`, kiosk `snackbox-maui`) export OpenTelemetry **traces + logs** via OTLP/gRPC to a self-hosted SigNoz (`src/Snackbox.AppHost/Signoz/docker-compose.yaml`, started by the AppHost `signoz` resource or `docker compose up -d` in that folder). UI: `http://localhost:3301`, collector: `4317` (gRPC) / `4318` (HTTP)
- **Shared setup**: `Snackbox.ServiceDefaults/TelemetryDefaults.cs` (`AddSnackboxOpenTelemetry` / `AddSnackboxOpenTelemetryLogging`). Extra OTLP targets come from `Telemetry:Otlp:AdditionalGrpcEndpoints`; the Aspire dashboard exporter is only added when `OTEL_EXPORTER_OTLP_ENDPOINT` is set (avoids duplicate export from the kiosk)
- **What is captured**: ASP.NET Core + HttpClient + EF/Npgsql spans with exceptions recorded; verbose per-request HTTP logging on the API (headers + request/response bodies, one record per request — `/api/auth/*` and `/api/testhelper/*` are excluded so credentials never get logged); UI spans (`ui.*`, see `UiTelemetry`) tagged with the user; kiosk unhandled/unobserved exceptions logged as Critical/Error (`MauiProgram.HookUnhandledExceptions`)
- **Per-purchase deep links**: every `BarcodeScan` stores the `TraceId` of the request that recorded it, and the scan span carries `purchase.id` / `user.id` / `barcode.code`. Admin → User Details → Purchases has a **🔍 SigNoz** link per purchase opening that trace (base URL from `Telemetry:SignozUrl`)
- **First-run gotcha**: SigNoz's collector is OpAMP-managed and runs *without any receivers* until an organization exists. Complete the first-time signup in the UI (or `POST /api/v1/register`) once; the collector then reloads with the real config within ~30s. Until then every exporter is silently talking to a closed port
- **Kiosk note**: MAUI does not run hosted services, so the `TracerProvider` is resolved explicitly at startup (in `HookUnhandledExceptions`) to start exporting
- **`[Traced]` attribute interceptor** (`Snackbox.ServiceDefaults/Tracing`): put `[Traced]` on a DI service class or a single method and every call becomes a span `{Type}.{Method}` (source `Snackbox.Traced`) with `param.*` tags, Ok/Error status and the exception recorded; `[Sensitive]` on a parameter masks it, `[NotTraced]` opts a method out, `LogReturnValue = true` adds the result. Implemented as an in-box `DispatchProxy` around interface registrations — no Metalama/PostSharp. Wiring: `services.AddTracedServices()` after all registrations (auto-discovers attributed implementation types); typed HttpClients / factory registrations need `services.AddTracing<TInterface>()`. Only interface calls through DI are intercepted (not internal calls or controllers, which ASP.NET instrumentation already covers)

#### 13. Look and feel (logineer)
- Snackbox runs at logineer, so it uses their brand from logineer.com, whose stylesheet names the colours itself: **logblue** `#0082CD` (primary actions, links, logo), **cyan** `#00BCD4`, **anthracite** `#191E1E` (text, sidebar, dark blocks). Bold white headlines over light body text, square outlined buttons, and the blue-to-black duotone of their hero for the kiosk screens
- All of it lives as tokens in `src/Snackbox.Components/wwwroot/brand.css` (`--brand-blue`, `--brand-anthracite`, `--brand-gradient`, ...), served as `_content/Snackbox.Components/brand.css` and loaded after Bootstrap by both the kiosk (`wwwroot/index.html`) and the website (`App.razor`). Use the tokens rather than hex values in new styles
- Bootstrap is 5.1, which has no CSS variables for buttons - `brand.css` overrides `.btn-primary`, `.text-primary` and friends explicitly. Their house font Brix Sans is licensed; the stack falls back to Segoe UI

### Key Business Rules
1. Stock quantities must be manually updated by admins; purchases do not automatically reduce shelf stock counts
2. Each employee has an account balance (payments minus purchases)
3. Different batches of the same product are tracked separately by best before date
4. Users can only see their own financial data, admins can see all users
5. Only one discount is applied per purchase (the one providing highest savings)

### Domain Model Concepts
- **User/Employee**: Person who can purchase snacks
- **Product**: A specific snack item with a barcode
- **Batch**: A group of the same product with a specific best before date
- **Purchase/Transaction**: A record of a user buying a product
- **Payment**: A record of a user adding money to their account
- **Stock Level**: Quantity available (storage vs. shelf)
- **Balance**: User's financial standing (payments - purchases)
- **Achievement**: A reward earned for specific purchasing behaviors
- **UserAchievement**: Record of an achievement earned by a user with timestamp
- **Discount**: A price reduction that can be applied to purchases

## Technical Overview

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
#endif
```

### Web vs Native Considerations
- Use `IJSRuntime` carefully (may not work in all contexts)
- Abstract platform-specific features behind interfaces
- Test on web and Windows platforms

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
        new Category { Id = 1, Name = "Electronics", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
        new Category { Id = 2, Name = "Books", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
        new Category { Id = 3, Name = "Clothing", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
    );

    // Seed sample items
    modelBuilder.Entity<Item>().HasData(
        new Item { Id = 1, Name = "Laptop", CategoryId = 1, Price = 999.99m, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
        new Item { Id = 2, Name = "Programming Book", CategoryId = 2, Price = 49.99m, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
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

var app = builder.Build();

// Seed database on startup (before app.Run)
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

app.Run();
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
- **Aspire**: Leverage built-in health checks and telemetry

## Deployment

### Development
- Run via Aspire AppHost for integrated experience
- Aspire dashboard provides monitoring and logs

### Production
- Deploy API as container or App Service
- Deploy web app as static site or Blazor Server
- Windows app as standalone executable or MSIX package
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
6. Test on web and Windows

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
