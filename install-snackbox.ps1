# Snackbox Installation Script
# PowerShell script to download and install Snackbox from GitHub releases

param(
    [string]$InstallPath = "$env:ProgramFiles\Snackbox",
    [string]$RepoOwner = "daniel-kuon",
    [string]$RepoName = "snackbox-claude",
    [string]$Version = "latest",
    [switch]$AllowDowngrade = $false,
    [switch]$StopRunning = $true,
    [switch]$RestartAppHost = $false,
    [switch]$CreateShortcut = $true,
    [switch]$AddToStartMenu = $true
)

$ErrorActionPreference = "Stop"

Write-Host "=== Snackbox Installer ===" -ForegroundColor Cyan
Write-Host ""

# Check if running as administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin -and $InstallPath.StartsWith($env:ProgramFiles)) {
    Write-Host "WARNING: Installing to Program Files requires administrator privileges." -ForegroundColor Yellow
    Write-Host "Please run PowerShell as Administrator or choose a different install location." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To install to your user directory instead, run:" -ForegroundColor Yellow
    Write-Host "  irm https://raw.githubusercontent.com/$RepoOwner/$RepoName/main/install-snackbox.ps1 | iex -InstallPath `"`$env:LOCALAPPDATA\Snackbox`"" -ForegroundColor Yellow
    exit 1
}

try {
    # Step 1: Fetch release info from GitHub
    $requestedTag = $null
    if (-not [string]::IsNullOrWhiteSpace($Version) -and $Version -ne "latest") {
        $requestedTag = $Version.Trim()
        if (-not $requestedTag.StartsWith("v")) {
            $requestedTag = "v$requestedTag"
        }
    }

    Write-Host "Fetching release information..." -ForegroundColor Green
    if ($requestedTag) {
        Write-Host "Target version: $requestedTag" -ForegroundColor Cyan
        $apiUrl = "https://api.github.com/repos/$RepoOwner/$RepoName/releases/tags/$requestedTag"
    } else {
        $apiUrl = "https://api.github.com/repos/$RepoOwner/$RepoName/releases/latest"
    }
    $headers = @{ "User-Agent" = "Snackbox-Installer" }

    $release = Invoke-RestMethod -Uri $apiUrl -Headers $headers
    $version = $release.tag_name.TrimStart('v')

    Write-Host "Release version: $version" -ForegroundColor Cyan
    Write-Host "Published: $($release.published_at)" -ForegroundColor Cyan
    Write-Host ""

    function Stop-SnackboxProcesses {
        $processNames = @("Snackbox.AppHost", "Snackbox.Api", "Snackbox.BlazorServer", "Snackbox.Web")
        $appHostWasRunning = $false

        foreach ($name in $processNames) {
            $processes = Get-Process -Name $name -ErrorAction SilentlyContinue
            if (-not $processes) {
                continue
            }

            foreach ($process in $processes) {
                if ($process.ProcessName -eq "Snackbox.AppHost") {
                    $appHostWasRunning = $true
                }

                if ($process.MainWindowHandle -ne 0) {
                    $null = $process.CloseMainWindow()
                    $process.WaitForExit(5000)
                }

                if (-not $process.HasExited) {
                    Stop-Process -Id $process.Id -Force
                }
            }
        }

        return $appHostWasRunning
    }

    # Step 1b: Prevent accidental downgrades unless explicitly allowed
    $appHostPath = Join-Path $InstallPath "Snackbox.AppHost.exe"
    if (Test-Path $appHostPath) {
        $currentVersionString = (Get-Item $appHostPath).VersionInfo.ProductVersion
        if ([string]::IsNullOrWhiteSpace($currentVersionString)) {
            $currentVersionString = (Get-Item $appHostPath).VersionInfo.FileVersion
        }

        if (-not [string]::IsNullOrWhiteSpace($currentVersionString)) {
            $cleanCurrentVersion = $currentVersionString.Split('+')[0]
            $cleanTargetVersion = $version.Split('+')[0]
            $currentParsed = $null
            $targetParsed = $null

            if ([System.Version]::TryParse($cleanCurrentVersion, [ref]$currentParsed) -and [System.Version]::TryParse($cleanTargetVersion, [ref]$targetParsed)) {
                if ($targetParsed -lt $currentParsed -and -not $AllowDowngrade) {
                    Write-Host "ERROR: Target version $version is older than current version $cleanCurrentVersion." -ForegroundColor Red
                    Write-Host "Re-run with -AllowDowngrade to confirm the downgrade." -ForegroundColor Yellow
                    exit 1
                }

                if ($targetParsed -eq $currentParsed) {
                    Write-Host "Snackbox is already on version $cleanCurrentVersion." -ForegroundColor Green
                    exit 0
                }
            }
        }
    }

    # Step 2: Find the Windows x64 package
    $asset = $release.assets | Where-Object { $_.name -like "snackbox-full-*-win-x64.zip" } | Select-Object -First 1

    if (-not $asset) {
        Write-Host "ERROR: Could not find Windows x64 package in release." -ForegroundColor Red
        exit 1
    }

    Write-Host "Package: $($asset.name)" -ForegroundColor Cyan
    Write-Host "Size: $([Math]::Round($asset.size / 1MB, 2)) MB" -ForegroundColor Cyan
    Write-Host ""

    # Step 3: Create temp directory and download
    $tempDir = Join-Path $env:TEMP "snackbox-install-$(New-Guid)"
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

    $downloadPath = Join-Path $tempDir $asset.name

    Write-Host "Downloading Snackbox..." -ForegroundColor Green
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $downloadPath -Headers $headers
    Write-Host "✓ Download complete" -ForegroundColor Green
    Write-Host ""

    # Step 3b: Stop running Snackbox processes if requested
    $appHostWasRunning = $false
    if ($StopRunning) {
        Write-Host "Stopping running Snackbox processes..." -ForegroundColor Yellow
        $appHostWasRunning = Stop-SnackboxProcesses
        Write-Host "✓ Processes stopped" -ForegroundColor Green
        Write-Host ""
    }

    # Step 4: Extract to install location
    Write-Host "Installing to: $InstallPath" -ForegroundColor Green

    if (Test-Path $InstallPath) {
        Write-Host "Installation directory already exists. Backing up..." -ForegroundColor Yellow
        $backupPath = "$InstallPath.backup.$(Get-Date -Format 'yyyyMMdd-HHmmss')"
        Move-Item -Path $InstallPath -Destination $backupPath -Force
        Write-Host "Backup created: $backupPath" -ForegroundColor Yellow
    }

    New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null

    Write-Host "Extracting files..." -ForegroundColor Green
    Expand-Archive -Path $downloadPath -DestinationPath $tempDir -Force

    # Find extracted directory (may be nested)
    $extractedDir = Get-ChildItem -Path $tempDir -Directory | Select-Object -First 1
    if ($extractedDir) {
        Copy-Item -Path "$($extractedDir.FullName)\*" -Destination $InstallPath -Recurse -Force
    } else {
        Copy-Item -Path "$tempDir\*" -Destination $InstallPath -Recurse -Force -Exclude "*.zip"
    }

    Write-Host "✓ Installation complete" -ForegroundColor Green
    Write-Host ""

    # Step 5: Create shortcuts
    if ($CreateShortcut) {
        Write-Host "Creating desktop shortcut..." -ForegroundColor Green
        $WshShell = New-Object -ComObject WScript.Shell
        $shortcutPath = Join-Path $env:USERPROFILE "Desktop\Snackbox.lnk"
        $shortcut = $WshShell.CreateShortcut($shortcutPath)
        $shortcut.TargetPath = Join-Path $InstallPath "Snackbox.AppHost.exe"
        $shortcut.WorkingDirectory = $InstallPath
        $shortcut.Description = "Snackbox - Employee Snack Management System"
        $shortcut.Save()
        Write-Host "✓ Desktop shortcut created" -ForegroundColor Green
    }

    if ($AddToStartMenu) {
        Write-Host "Adding to Start Menu..." -ForegroundColor Green
        $startMenuPath = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\Snackbox.lnk"
        $WshShell = New-Object -ComObject WScript.Shell
        $shortcut = $WshShell.CreateShortcut($startMenuPath)
        $shortcut.TargetPath = Join-Path $InstallPath "Snackbox.AppHost.exe"
        $shortcut.WorkingDirectory = $InstallPath
        $shortcut.Description = "Snackbox - Employee Snack Management System"
        $shortcut.Save()
        Write-Host "✓ Start Menu entry created" -ForegroundColor Green
    }

    # Step 6: Cleanup
    Write-Host ""
    Write-Host "Cleaning up..." -ForegroundColor Green
    Remove-Item -Path $tempDir -Recurse -Force

    # Step 7: Restart AppHost if requested
    if ($RestartAppHost -and $appHostWasRunning) {
        $appHostExe = Join-Path $InstallPath "Snackbox.AppHost.exe"
        if (Test-Path $appHostExe) {
            Write-Host ""
            Write-Host "Restarting Snackbox AppHost..." -ForegroundColor Green
            Start-Process -FilePath $appHostExe -WorkingDirectory $InstallPath
        }
    }

    # Step 7: Success message
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "✓ Snackbox installed successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Installation Location: $InstallPath" -ForegroundColor Cyan
    Write-Host "Version: $version" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "To launch Snackbox:" -ForegroundColor White
    Write-Host "  1. Use the desktop shortcut" -ForegroundColor White
    Write-Host "  2. Search for 'Snackbox' in Start Menu" -ForegroundColor White
    Write-Host "  3. Run: $InstallPath\Snackbox.AppHost.exe" -ForegroundColor White
    Write-Host ""
    Write-Host "To update later:" -ForegroundColor White
    Write-Host "  Re-run this installer (optionally with -Version and -AllowDowngrade)" -ForegroundColor White
    Write-Host ""
    Write-Host "Enjoy! 🍿" -ForegroundColor Cyan

} catch {
    Write-Host ""
    Write-Host "ERROR: Installation failed" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Please report this issue at: https://github.com/$RepoOwner/$RepoName/issues" -ForegroundColor Yellow
    exit 1
}
