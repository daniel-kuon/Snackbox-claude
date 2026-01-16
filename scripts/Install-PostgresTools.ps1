<#
.SYNOPSIS
    Installs PostgreSQL 17 for Snackbox backup functionality.

.DESCRIPTION
    This script installs PostgreSQL 17 using winget (Windows Package Manager).
    PostgreSQL client tools (pg_dump, psql) are required for Snackbox database
    backup and restore operations.

    The script:
    - Checks if winget is available
    - Detects if PostgreSQL tools are already installed
    - Installs PostgreSQL 17 via winget if needed
    - Verifies installation

.EXAMPLE
    .\Install-PostgresTools.ps1
    Installs PostgreSQL 17 using winget.

.NOTES
    Requires Windows Package Manager (winget) to be installed.
    winget typically requires user interaction for installation.
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

# Color output functions
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Write-Success {
    param([string]$Message)
    Write-ColorOutput "✓ $Message" "Green"
}

function Write-Info {
    param([string]$Message)
    Write-ColorOutput "ℹ $Message" "Cyan"
}

function Write-Warning {
    param([string]$Message)
    Write-ColorOutput "⚠ $Message" "Yellow"
}

function Write-ErrorMessage {
    param([string]$Message)
    Write-ColorOutput "✗ $Message" "Red"
}

# Check if winget is available
function Test-Winget {
    try {
        $wingetVersion = & winget --version 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "winget is available: $wingetVersion"
            return $true
        }
    } catch {
        return $false
    }
    return $false
}

# Check if PostgreSQL tools are installed
function Test-PostgreSQLTools {
    try {
        $pgDumpVersion = & pg_dump --version 2>&1
        $psqlVersion = & psql --version 2>&1

        if ($LASTEXITCODE -eq 0) {
            Write-Success "PostgreSQL tools are already installed:"
            Write-Host "  - $pgDumpVersion"
            Write-Host "  - $psqlVersion"

            # Check if it's version 17.x
            if ($pgDumpVersion -match "(\d+)\.\d+") {
                $majorVersion = $matches[1]
                if ($majorVersion -eq "17") {
                    return $true
                } else {
                    Write-Warning "PostgreSQL version $majorVersion is installed, but version 17 is required"
                    return $false
                }
            }

            return $true
        }
    } catch {
        return $false
    }
    return $false
}

# Main installation function
function Install-PostgreSQLTools {
    Write-ColorOutput "`n==================================" "Cyan"
    Write-ColorOutput "PostgreSQL 17 Installer" "Cyan"
    Write-ColorOutput "==================================`n" "Cyan"

    # Check if already installed with correct version
    if (Test-PostgreSQLTools) {
        Write-Success "No installation needed. PostgreSQL 17 tools are already available.`n"
        return
    }

    Write-Info "PostgreSQL 17 tools not found or version mismatch. Starting installation...`n"

    # Check if winget is available
    if (-not (Test-Winget)) {
        Write-ErrorMessage "winget (Windows Package Manager) is not available"
        Write-Host "`nPlease install winget first:" -ForegroundColor Yellow
        Write-Host "  1. Open Microsoft Store" -ForegroundColor Yellow
        Write-Host "  2. Search for 'App Installer'" -ForegroundColor Yellow
        Write-Host "  3. Install or update it" -ForegroundColor Yellow
        Write-Host "`nOr download from: https://aka.ms/getwinget`n" -ForegroundColor Yellow
        throw "winget is not available"
    }

    # Install PostgreSQL 17 using winget
    Write-Info "Installing PostgreSQL 17 via winget..."
    Write-Host "  Package: PostgreSQL.PostgreSQL.17" -ForegroundColor Gray
    Write-Host "`n  Note: You may need to accept the package agreement during installation.`n" -ForegroundColor Yellow

    try {
        $installProcess = Start-Process -FilePath "winget" -ArgumentList "install", "-e", "--id", "PostgreSQL.PostgreSQL.17" -Wait -PassThru -NoNewWindow

        if ($installProcess.ExitCode -eq 0) {
            Write-Success "PostgreSQL 17 installed successfully"
        } elseif ($installProcess.ExitCode -eq -1978335189) {
            Write-Warning "Package already installed or pending upgrade"
        } else {
            Write-ErrorMessage "winget installation failed with exit code: $($installProcess.ExitCode)"
            throw "Installation failed"
        }
    } catch {
        Write-ErrorMessage "Failed to install PostgreSQL via winget"
        Write-Host "  Error: $_" -ForegroundColor Red
        Write-Host "`nManual installation steps:" -ForegroundColor Yellow
        Write-Host "  Run: winget install -e --id PostgreSQL.PostgreSQL.17" -ForegroundColor Yellow
        Write-Host "  Or visit: https://www.postgresql.org/download/windows/" -ForegroundColor Yellow
        throw
    }

    # Verify installation
    Write-Info "`nVerifying installation..."
    Write-Info "Refreshing PATH environment..."

    # Refresh PATH for current session (include both machine and user paths)
    $env:PATH = [System.Environment]::GetEnvironmentVariable("PATH", "Machine") + ";" +
                [System.Environment]::GetEnvironmentVariable("PATH", "User")

    # Wait a moment for installation to complete
    Start-Sleep -Seconds 2

    if (Test-PostgreSQLTools) {
        Write-ColorOutput "`n==================================`n" "Green"
        Write-Success "Installation completed successfully!"
        Write-ColorOutput "==================================`n" "Green"
        Write-Host "PostgreSQL 17 tools are now ready for use with Snackbox.`n" -ForegroundColor Cyan
    } else {
        Write-Warning "`nInstallation completed but tools are not yet available in PATH."
        Write-Warning "Please restart your terminal to refresh the PATH environment variable.`n"
        Write-Host "Default PostgreSQL installation location:" -ForegroundColor Yellow
        Write-Host "  C:\Program Files\PostgreSQL\17\bin`n" -ForegroundColor Yellow
    }
}

# Run installation
try {
    Install-PostgreSQLTools
} catch {
    Write-ErrorMessage "`nInstallation failed: $_"
    Write-Host "`nFor manual installation, visit:" -ForegroundColor Yellow
    Write-Host "  https://www.postgresql.org/download/windows/`n" -ForegroundColor Yellow
    exit 1
}
