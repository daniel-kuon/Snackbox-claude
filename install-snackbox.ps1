# Snackbox installer.
#
# A Snackbox installation is a git checkout of the repository that is built and run in place:
# the Aspire AppHost orchestrates Postgres and SigNoz in Docker, so the machine needs git, the
# .NET SDK and Docker Desktop. Updating later is the same checkout moved to a newer release
# tag, either from the Updates page in the app or with tools/Snackbox.Updater.
#
#   irm https://raw.githubusercontent.com/daniel-kuon/Snackbox-claude/main/install-snackbox.ps1 | iex
#
# or, with options:
#
#   .\install-snackbox.ps1 -InstallPath C:\Snackbox -NoKiosk

param(
    [string]$InstallPath = "C:\Snackbox",
    [string]$Repository = "daniel-kuon/Snackbox-claude",

    # Register the logon task but leave the kiosk window out of it - useful while the old
    # Snackbox is still the one on screen.
    [switch]$NoKiosk,

    # Install and build, but do not register autostart or start anything.
    [switch]$NoStart
)

$ErrorActionPreference = "Stop"

function Write-Step($message) { Write-Host "==> $message" -ForegroundColor Cyan }
function Write-Ok($message) { Write-Host "    $message" -ForegroundColor Green }

Write-Host "=== Snackbox installer ===" -ForegroundColor Cyan
Write-Host ""

# --------------------------------------------------------------- prerequisites

Write-Step "Checking prerequisites"

foreach ($tool in @("git", "dotnet")) {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
        throw "$tool was not found on PATH. Install it and run this script again."
    }
    Write-Ok "$tool found"
}

$dockerPresent = $null -ne (Get-Command docker -ErrorAction SilentlyContinue)
if (-not $dockerPresent) {
    Write-Host "    WARNING: docker was not found. Snackbox needs Docker Desktop for its database." -ForegroundColor Yellow
    Write-Host "             Install it from https://www.docker.com/products/docker-desktop/" -ForegroundColor Yellow
} else {
    Write-Ok "docker found"
}

# ------------------------------------------------------------------- checkout

$cloneUrl = "https://github.com/$Repository.git"

if (Test-Path (Join-Path $InstallPath ".git")) {
    Write-Step "Using the existing checkout at $InstallPath"
    git -C $InstallPath remote set-url origin $cloneUrl
    git -C $InstallPath fetch --tags --prune origin
} else {
    if ((Test-Path $InstallPath) -and (Get-ChildItem $InstallPath -Force | Select-Object -First 1)) {
        throw "$InstallPath already exists and is not a Snackbox checkout. Pick another -InstallPath."
    }

    Write-Step "Cloning $Repository into $InstallPath"
    git clone $cloneUrl $InstallPath
}

# Pick the newest release tag; fall back to the default branch when nothing is released yet.
$tags = git -C $InstallPath tag --list "v*.*.*" --sort=-v:refname
$targetTag = if ($tags) { ($tags -split "`n")[0].Trim() } else { $null }

if ($targetTag) {
    Write-Step "Checking out $targetTag"
    git -C $InstallPath -c advice.detachedHead=false checkout --force "tags/$targetTag"
} else {
    Write-Host "    No release tags yet - staying on the default branch." -ForegroundColor Yellow
    git -C $InstallPath checkout --force main
    git -C $InstallPath pull --ff-only
}

# ---------------------------------------------------------------------- build

Write-Step "Building (Release) - this takes a few minutes"
Push-Location $InstallPath
try {
    # No separate restore: one without -c Release skips the runtime packs the kiosk's
    # Release build needs, which fails on any machine that has not built it before.
    dotnet build Snackbox.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }
}
finally {
    Pop-Location
}
Write-Ok "Build finished"

$updater = Join-Path $InstallPath "tools\Snackbox.Updater\bin\Release\net10.0\Snackbox.Updater.exe"
if (-not (Test-Path $updater)) { throw "The updater was not built: $updater is missing." }

if ($NoStart) {
    Write-Host ""
    Write-Host "Installed to $InstallPath without starting anything." -ForegroundColor Green
    Write-Host "Start it later with: `"$updater`" start" -ForegroundColor Gray
    return
}

# ------------------------------------------------------------------ autostart

Write-Step "Registering autostart (scheduled task at logon)"
$autostartArgs = @("install-autostart", "--dir", $InstallPath)
if ($NoKiosk) { $autostartArgs += "--no-kiosk" }
& $updater @autostartArgs
if ($LASTEXITCODE -ne 0) { throw "Registering autostart failed." }

# ---------------------------------------------------------------------- start

Write-Step "Starting Snackbox"
$startArgs = @("start", "--dir", $InstallPath)
if ($NoKiosk) { $startArgs += "--no-kiosk" }
& $updater @startArgs

Write-Host ""
Write-Host "Snackbox is installed in $InstallPath." -ForegroundColor Green
Write-Host "It starts automatically at logon; updates are on the Updates page in the admin area." -ForegroundColor Green
Write-Host "Updater log: $(Join-Path $InstallPath '.snackbox\updater.log')" -ForegroundColor Gray
