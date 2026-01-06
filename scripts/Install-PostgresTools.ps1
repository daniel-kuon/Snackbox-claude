<#
.SYNOPSIS
    Installs PostgreSQL client tools for Snackbox backup functionality.

.DESCRIPTION
    This script downloads and installs PostgreSQL client tools (pg_dump, psql) 
    required for Snackbox database backup and restore operations.
    
    The script:
    - Detects if PostgreSQL tools are already installed
    - Downloads PostgreSQL binaries if not present
    - Extracts only the required client tools
    - Adds tools to the system PATH
    - Verifies installation

.PARAMETER InstallPath
    The directory where PostgreSQL tools will be installed.
    Default: C:\Program Files\PostgreSQL\Tools

.PARAMETER PostgreSQLVersion
    The version of PostgreSQL to download.
    Default: 16.1 (latest stable as of script creation)

.PARAMETER SkipPathUpdate
    Skip adding tools to system PATH.

.EXAMPLE
    .\Install-PostgresTools.ps1
    Installs PostgreSQL tools to default location.

.EXAMPLE
    .\Install-PostgresTools.ps1 -InstallPath "C:\Tools\PostgreSQL"
    Installs PostgreSQL tools to custom location.

.NOTES
    Requires Administrator privileges to add to system PATH.
    For non-admin users, PATH will be added to user scope only.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [string]$InstallPath = "C:\Program Files\PostgreSQL\Tools",
    
    [Parameter(Mandatory=$false)]
    [string]$PostgreSQLVersion = "16.1",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipPathUpdate
)

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

# Check if running as administrator
function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# Check if tools are already installed
function Test-PostgreSQLTools {
    try {
        $pgDumpVersion = & pg_dump --version 2>&1
        $psqlVersion = & psql --version 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "PostgreSQL tools are already installed:"
            Write-Host "  - $pgDumpVersion"
            Write-Host "  - $psqlVersion"
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
    Write-ColorOutput "PostgreSQL Tools Installer" "Cyan"
    Write-ColorOutput "==================================`n" "Cyan"

    # Check if already installed
    if (Test-PostgreSQLTools) {
        Write-Success "No installation needed. PostgreSQL tools are already available.`n"
        return
    }

    Write-Info "PostgreSQL tools not found. Starting installation...`n"

    # Check admin rights
    $isAdmin = Test-Administrator
    if (-not $isAdmin) {
        Write-Warning "Not running as Administrator. Will install to user directory and update user PATH only."
        $InstallPath = "$env:LOCALAPPDATA\PostgreSQL\Tools"
    }

    # Create installation directory
    Write-Info "Creating installation directory: $InstallPath"
    if (-not (Test-Path $InstallPath)) {
        New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
    }
    Write-Success "Installation directory created"

    # Download PostgreSQL portable binaries
    $architecture = if ([Environment]::Is64BitOperatingSystem) { "x64" } else { "x86" }
    $downloadUrl = "https://get.enterprisedb.com/postgresql/postgresql-$PostgreSQLVersion-1-windows-$architecture-binaries.zip"
    $zipFile = Join-Path $env:TEMP "postgresql-binaries.zip"
    $extractPath = Join-Path $env:TEMP "postgresql-extract"

    Write-Info "Downloading PostgreSQL binaries..."
    Write-Host "  URL: $downloadUrl" -ForegroundColor Gray
    
    try {
        # Use WebClient for better progress
        $webClient = New-Object System.Net.WebClient
        $webClient.DownloadFile($downloadUrl, $zipFile)
        Write-Success "Download completed"
    } catch {
        Write-ErrorMessage "Failed to download PostgreSQL binaries"
        Write-Host "  Error: $_" -ForegroundColor Red
        Write-Host "`nManual installation steps:" -ForegroundColor Yellow
        Write-Host "  1. Download PostgreSQL from: https://www.postgresql.org/download/windows/" -ForegroundColor Yellow
        Write-Host "  2. Install PostgreSQL (you can use default settings)" -ForegroundColor Yellow
        Write-Host "  3. Add PostgreSQL bin directory to PATH" -ForegroundColor Yellow
        throw
    }

    # Extract binaries
    Write-Info "Extracting binaries..."
    try {
        Expand-Archive -Path $zipFile -DestinationPath $extractPath -Force
        Write-Success "Extraction completed"
    } catch {
        Write-ErrorMessage "Failed to extract binaries: $_"
        throw
    }

    # Copy only required tools
    Write-Info "Installing client tools..."
    $binPath = Get-ChildItem -Path $extractPath -Filter "bin" -Recurse -Directory | Select-Object -First 1
    
    if ($null -eq $binPath) {
        Write-ErrorMessage "Could not find bin directory in extracted files"
        throw "Installation failed: bin directory not found"
    }

    $requiredTools = @(
        "pg_dump.exe",
        "psql.exe",
        "libpq.dll",
        "libintl-9.dll",
        "libiconv-2.dll",
        "libcrypto-3-x64.dll",
        "libssl-3-x64.dll"
    )

    foreach ($tool in $requiredTools) {
        $sourcePath = Join-Path $binPath.FullName $tool
        $destPath = Join-Path $InstallPath $tool
        
        if (Test-Path $sourcePath) {
            Copy-Item -Path $sourcePath -Destination $destPath -Force
            Write-Host "  ✓ Installed: $tool" -ForegroundColor Gray
        } else {
            Write-Warning "Optional file not found: $tool"
        }
    }
    Write-Success "Client tools installed to: $InstallPath"

    # Clean up
    Write-Info "Cleaning up temporary files..."
    Remove-Item $zipFile -Force -ErrorAction SilentlyContinue
    Remove-Item $extractPath -Recurse -Force -ErrorAction SilentlyContinue
    Write-Success "Cleanup completed"

    # Update PATH
    if (-not $SkipPathUpdate) {
        Write-Info "Updating PATH environment variable..."
        
        try {
            $pathScope = if ($isAdmin) { "Machine" } else { "User" }
            $currentPath = [Environment]::GetEnvironmentVariable("PATH", $pathScope)
            
            if ($currentPath -notlike "*$InstallPath*") {
                $newPath = "$currentPath;$InstallPath"
                [Environment]::SetEnvironmentVariable("PATH", $newPath, $pathScope)
                
                # Update current session
                $env:PATH = "$env:PATH;$InstallPath"
                
                Write-Success "PATH updated ($pathScope scope)"
                Write-Warning "You may need to restart your terminal for PATH changes to take effect."
            } else {
                Write-Success "PATH already contains PostgreSQL tools directory"
            }
        } catch {
            Write-Warning "Failed to update PATH automatically: $_"
            Write-Host "`nPlease manually add this directory to your PATH:" -ForegroundColor Yellow
            Write-Host "  $InstallPath" -ForegroundColor Yellow
        }
    }

    # Verify installation
    Write-Info "`nVerifying installation..."
    
    # Refresh PATH for current session
    $env:PATH = [System.Environment]::GetEnvironmentVariable("PATH", "Machine") + ";" + 
                [System.Environment]::GetEnvironmentVariable("PATH", "User")
    
    Start-Sleep -Seconds 1
    
    if (Test-PostgreSQLTools) {
        Write-ColorOutput "`n==================================`n" "Green"
        Write-Success "Installation completed successfully!"
        Write-ColorOutput "==================================`n" "Green"
        Write-Host "PostgreSQL tools are now ready for use with Snackbox.`n" -ForegroundColor Cyan
    } else {
        Write-Warning "`nInstallation completed but tools are not yet in PATH."
        Write-Warning "Please restart your terminal or add this to your PATH manually:"
        Write-Host "  $InstallPath`n" -ForegroundColor Yellow
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
