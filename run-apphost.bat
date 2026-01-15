@echo off
REM Batch file to run the Snackbox AppHost executable
REM This sets up the necessary environment and runs the AppHost

echo Starting Snackbox AppHost...
echo.

REM Set the working directory to the AppHost binary location
cd /d "%~dp0src\Snackbox.AppHost\bin\Debug\net10.0"

REM Optional: Set custom dashboard URLs if needed (these are now configured in code as defaults)
set ASPNETCORE_URLS=https://localhost:18888
set ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL=http://localhost:18889
set ASPIRE_ALLOW_UNSECURED_TRANSPORT=true

REM Run the AppHost executable
Snackbox.AppHost.exe

REM Pause to see any error messages
pause
