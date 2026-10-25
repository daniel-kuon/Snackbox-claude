@echo off
REM Starts Snackbox the way autostart does: Docker, the Aspire AppHost (Release build, run as
REM Production) and the kiosk window. Whatever still runs is stopped first, so this also
REM restarts a stack that hangs. Starting the AppHost on its own leaves the kiosk out.
set UPDATER=%~dp0tools\Snackbox.Updater\bin\Release\net10.0\Snackbox.Updater.exe
if not exist "%UPDATER%" (
    echo Snackbox is not built yet: dotnet build Snackbox.sln -c Release
    pause
    exit /b 1
)
"%UPDATER%" stop --dir "%~dp0."
"%UPDATER%" start --dir "%~dp0."
if errorlevel 1 pause
