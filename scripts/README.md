# Snackbox Setup Scripts

This directory contains scripts to help set up and configure Snackbox.

## Install-PostgresTools.ps1

Installs PostgreSQL client tools (pg_dump, psql) required for database backup and restore functionality.

### Usage

**Basic Installation** (Recommended):
```powershell
.\scripts\Install-PostgresTools.ps1
```

**Custom Installation Path**:
```powershell
.\scripts\Install-PostgresTools.ps1 -InstallPath "C:\Tools\PostgreSQL"
```

**Skip PATH Update** (if you want to add to PATH manually):
```powershell
.\scripts\Install-PostgresTools.ps1 -SkipPathUpdate
```

### What It Does

1. Checks if PostgreSQL tools are already installed
2. Downloads PostgreSQL portable binaries from the official source
3. Extracts only the required client tools (pg_dump, psql, and dependencies)
4. Installs tools to a specified directory (default: `C:\Program Files\PostgreSQL\Tools`)
5. Adds the tools directory to your system PATH
6. Verifies the installation

### Requirements

- Windows PowerShell 5.1 or later
- Internet connection to download PostgreSQL binaries
- Administrator privileges (recommended) - script will work without admin but installs to user directory

### Notes

- **Administrator Mode**: Run PowerShell as Administrator for system-wide installation
- **User Mode**: Without admin rights, tools install to `%LOCALAPPDATA%\PostgreSQL\Tools`
- **PATH Changes**: You may need to restart your terminal after installation
- **Version**: By default, installs PostgreSQL 16.1 client tools

### Troubleshooting

**If the script fails to download:**
- Check your internet connection
- Verify you can access https://get.enterprisedb.com
- Try manual installation from https://www.postgresql.org/download/windows/

**If tools aren't found after installation:**
- Close and reopen your terminal
- Verify the installation directory is in your PATH
- Try running: `$env:PATH` to see current PATH

**To verify installation:**
```powershell
pg_dump --version
psql --version
```

### Manual Installation Alternative

If the script doesn't work for your environment:

1. Download PostgreSQL from: https://www.postgresql.org/download/windows/
2. Run the installer
3. During installation, ensure "Command Line Tools" is selected
4. After installation, PostgreSQL tools will be available in your PATH

## Alternative: Using Chocolatey

If you have Chocolatey installed, you can install PostgreSQL tools with:

```powershell
choco install postgresql --params '/Password:dummypassword /NoPath:false'
```

## Alternative: Using Scoop

If you have Scoop installed:

```powershell
scoop install postgresql
```

## Checking Tool Availability

You can check if PostgreSQL tools are available via the Snackbox API:

```bash
curl http://localhost:5000/api/backup/tools/check
```

Or through the admin interface at: Admin > Backups (will show a warning if tools are missing)
