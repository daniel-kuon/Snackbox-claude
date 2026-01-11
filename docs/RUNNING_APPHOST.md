# Running Snackbox AppHost as Executable

## Overview
The Snackbox AppHost can now be run as a standalone executable file. The dashboard has been configured with default URLs to support this execution mode.

## Configuration

### Dashboard Endpoints
The following default endpoints are configured:
- **Dashboard UI**: http://localhost:18888
- **OTLP Endpoint**: http://localhost:18889

These are automatically set when running the executable if the environment variables are not already configured.

## Running the AppHost

### Option 1: Using the Batch File (Recommended)
Simply double-click or run:
```
run-apphost.bat
```

### Option 2: Manual Execution
1. Build the project:
   ```
   dotnet build src\Snackbox.AppHost\Snackbox.AppHost.csproj
   ```

2. Navigate to the output directory:
   ```
   cd src\Snackbox.AppHost\bin\Debug\net10.0
   ```

3. Run the executable:
   ```
   Snackbox.AppHost.exe
   ```

### Option 3: Using dotnet run
From the solution root:
```
dotnet run --project src\Snackbox.AppHost
```

## Custom Configuration

### Environment Variables
If you need to override the default dashboard URLs, set these environment variables before running:

```cmd
set ASPNETCORE_URLS=http://localhost:YOUR_PORT
set ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL=http://localhost:YOUR_OTLP_PORT
```

### Configuration Files
You can also modify the following files to change default behavior:
- `src\Snackbox.AppHost\appsettings.json` - Production settings
- `src\Snackbox.AppHost\appsettings.Development.json` - Development settings

## Troubleshooting

### Dashboard Not Starting
If you see errors about missing environment variables:
1. Verify that `appsettings.json` exists in the AppHost directory
2. Check that the dashboard configuration in `Program.cs` is present
3. Ensure ports 18888 and 18889 are not already in use

### Port Conflicts
If the default ports are in use, either:
1. Stop the process using those ports
2. Change the ports in `appsettings.json`
3. Set custom environment variables as shown above

## Accessing the Dashboard
Once running, access the Aspire Dashboard at:
**http://localhost:18888**

## Prerequisites
- .NET 10.0 SDK installed
- Docker Desktop running (for PostgreSQL and other containers)
- All NuGet packages restored

## What Gets Started
When you run the AppHost, it orchestrates:
- PostgreSQL database container
- pgAdmin container
- Snackbox API project
- Snackbox Blazor Server web application
- Aspire Dashboard for monitoring

All resources can be monitored through the dashboard UI.
