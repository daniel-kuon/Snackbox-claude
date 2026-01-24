# Snackbox Installation & Update Guide

This guide covers installing and updating Snackbox on Windows.

## Quick Installation

### One-Line Installation (Recommended)

Open **PowerShell as Administrator** and run:

```powershell
irm https://raw.githubusercontent.com/YOUR_GITHUB_USERNAME/snackbox-claude/main/install-snackbox.ps1 | iex
```

> **Important**: Replace `YOUR_GITHUB_USERNAME` with your actual GitHub username

This will:
- Download the latest release from GitHub
- Install to `C:\Program Files\Snackbox`
- Create desktop and Start Menu shortcuts
- Include the Snackbox Updater tool

### User Directory Installation (No Admin Required)

```powershell
$params = @{ InstallPath = "$env:LOCALAPPDATA\Snackbox" }
irm https://raw.githubusercontent.com/YOUR_GITHUB_USERNAME/snackbox-claude/main/install-snackbox.ps1 | iex @params
```

## Manual Installation

1. Go to [GitHub Releases](https://github.com/YOUR_GITHUB_USERNAME/snackbox-claude/releases)
2. Download `snackbox-full-{version}-win-x64.zip`
3. Extract to your preferred location
4. Run `Snackbox.AppHost.exe`

## System Requirements

- **Operating System**: Windows 10 or Windows 11 (64-bit)
- **RAM**: 4GB minimum, 8GB recommended
- **Disk Space**: 500MB for application + space for PostgreSQL data
- **.NET Runtime**: Not required (self-contained)

## First Launch

1. **Launch Snackbox**:
   - Double-click the desktop shortcut, OR
   - Search for "Snackbox" in Start Menu, OR
   - Run `Snackbox.AppHost.exe` from installation directory

2. **Aspire Dashboard**: The Aspire dashboard will open automatically at `http://localhost:18888`

3. **Access the Application**:
   - **Web Interface**: http://localhost:5001
   - **Windows App**: Runs automatically in MAUI window

4. **Default Credentials**: *(Configure based on your setup)*
   - Username: `admin`
   - Password: `admin`

## Updating Snackbox

### Method 1: In-App Update (Recommended)

1. Launch Snackbox
2. Navigate to the admin menu
3. Click **"Check for Updates"**
4. If an update is available:
   - Review the changelog
   - Click "Yes" to install
5. Wait for the update to complete
6. Restart Snackbox

### Method 2: Command Line Update

```powershell
cd "C:\Program Files\Snackbox"
.\Snackbox.Updater.exe
```

**Command-line options**:
```powershell
# Check for updates without installing
.\Snackbox.Updater.exe --check-only

# Silent update (no prompts)
.\Snackbox.Updater.exe --silent

# Show help
.\Snackbox.Updater.exe --help
```

### Method 3: Manual Update

1. Stop Snackbox (close all windows)
2. Download the latest release from GitHub
3. Extract `snackbox-full-{version}-win-x64.zip`
4. Copy and replace files in your installation directory
5. Restart Snackbox

## Update Safety Features

The updater includes several safety mechanisms:

- **Automatic Backup**: Creates backup before applying updates
- **Checksum Verification**: Validates download integrity (SHA256)
- **Rollback on Failure**: Restores backup if update fails
- **Process Management**: Gracefully stops AppHost before updating

## Uninstalling Snackbox

### Windows

1. Close Snackbox
2. Delete the installation folder (e.g., `C:\Program Files\Snackbox`)
3. Delete shortcuts:
   - Desktop: `%USERPROFILE%\Desktop\Snackbox.lnk`
   - Start Menu: `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Snackbox.lnk`
4. *(Optional)* Delete PostgreSQL data directory if you want to remove all data

## Troubleshooting

### Installation Issues

**Problem**: "Access denied" during installation
**Solution**: Run PowerShell as Administrator or install to user directory

**Problem**: Antivirus blocks the installer
**Solution**: Add an exception for the installer and installation directory

**Problem**: Download fails
**Solution**: Check internet connection and GitHub availability

### Update Issues

**Problem**: Update fails with "AppHost still running"
**Solution**: Manually close Snackbox.AppHost.exe in Task Manager

**Problem**: Checksum verification fails
**Solution**: Download may be corrupted, try again or download manually

**Problem**: Update rollback occurred
**Solution**: Check error logs and try manual update or reinstall

### Runtime Issues

**Problem**: Port already in use (18888, 5001)
**Solution**: Close conflicting applications or configure custom ports in `appsettings.json`

**Problem**: PostgreSQL connection fails
**Solution**: Ensure Docker Desktop is running or PostgreSQL service is started

## Configuration

### Custom Ports

Edit `src/Snackbox.AppHost/appsettings.json`:

```json
{
  "DashboardUrl": "http://localhost:18888",
  "ApiUrl": "http://localhost:5000"
}
```

### Data Directory

PostgreSQL data is stored in:
- **Docker**: Managed by Docker Desktop
- **Manual**: Configuration in connection string

## Advanced Topics

### Building from Source

See [CLAUDE.md](../CLAUDE.md) for development setup instructions.

### Creating Custom Releases

See [GitHub Release Workflow](../.github/workflows/release.yml) for automation details.

### Network Deployment

For multi-machine setups:
1. Install PostgreSQL on a dedicated server
2. Update connection strings in all clients
3. Configure API to accept external connections
4. Set up reverse proxy (optional)

## Support

- **Documentation**: [Project Documentation](../README.md)
- **Issues**: [GitHub Issues](https://github.com/YOUR_GITHUB_USERNAME/snackbox-claude/issues)
- **Discussions**: [GitHub Discussions](https://github.com/YOUR_GITHUB_USERNAME/snackbox-claude/discussions)

## Version History

Check [GitHub Releases](https://github.com/YOUR_GITHUB_USERNAME/snackbox-claude/releases) for version history and changelogs.
