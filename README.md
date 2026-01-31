# 🍿 Snackbox

**Snackbox** is a modern employee snack purchasing and inventory management system built with .NET 10, Blazor, and .NET Aspire. It streamlines snack purchases through a self-service barcode scanning system while maintaining accurate financial tracking and inventory management.

## ✨ Features

- 📱 **Self-Service Purchases** - Barcode scanning for quick snack purchases
- 💰 **Financial Tracking** - Track spending, payments, and account balances
- 📦 **Inventory Management** - Two-tier stock system (storage & shelf)
- 🎯 **Batch Management** - Track products by best-before dates
- 🏆 **Achievement System** - Gamified purchasing experience
- 👥 **Role-Based Access** - Separate admin and user permissions
- 🌐 **Cross-Platform** - Windows native app and web interface

## 🚀 Quick Start

### Installation (Windows)

Run this command in **PowerShell** (as Administrator recommended):

```powershell
irm https://raw.githubusercontent.com/YOUR_GITHUB_USERNAME/snackbox-claude/main/install-snackbox.ps1 | iex
```

> **Note**: Replace `YOUR_GITHUB_USERNAME` with your actual GitHub username

The installer will:
- Download the latest release from GitHub
- Extract files to `C:\Program Files\Snackbox`
- Create desktop and Start Menu shortcuts

### Alternative Installation

**User Directory Installation** (no admin required):
```powershell
$installParams = @{ InstallPath = "$env:LOCALAPPDATA\Snackbox" }
irm https://raw.githubusercontent.com/YOUR_GITHUB_USERNAME/snackbox-claude/main/install-snackbox.ps1 | iex
```

**Manual Installation**:
1. Download the latest `snackbox-full-{version}-win-x64.zip` from [Releases](https://github.com/YOUR_GITHUB_USERNAME/snackbox-claude/releases)
2. Extract to your preferred location
3. Run `Snackbox.AppHost.exe`

## 🔄 Updating

### In-App (Recommended)
1. Launch Snackbox
2. Click **"Check for Updates"** in the menu (admin only)
3. Start the update if a new version is available

### Manual Update
1. Download the latest release
2. Stop Snackbox AppHost
3. Extract and replace files
4. Restart application

## 💻 Development Setup

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for PostgreSQL)
- [Visual Studio 2024](https://visualstudio.microsoft.com/) or [JetBrains Rider](https://www.jetbrains.com/rider/)

### Required Workloads
```bash
dotnet workload install aspire
dotnet workload install maui
```

### Clone and Run
```bash
git clone https://github.com/YOUR_GITHUB_USERNAME/snackbox-claude.git
cd snackbox-claude
dotnet restore
dotnet run --project src/Snackbox.AppHost
```

The Aspire Dashboard will open automatically at `http://localhost:18888`

### Project Structure
```
snackbox-claude/
├── src/
│   ├── Snackbox.Api/              # Backend API (.NET 10)
│   ├── Snackbox.AppHost/          # Aspire orchestration
│   ├── Snackbox.BlazorServer/     # Web UI (Blazor Server)
│   ├── Snackbox.Web/              # Windows native app (MAUI)
│   ├── Snackbox.Components/       # Shared Blazor components
│   ├── Snackbox.ApiClient/        # API client library
│   ├── Snackbox.Api.Dtos/         # Shared DTOs
│   └── Snackbox.ServiceDefaults/  # Aspire defaults
├── tests/
│   ├── Snackbox.Api.Tests/        # API unit tests
│   └── Snackbox.Components.Tests/ # Component tests (bUnit)
└── docs/                          # Documentation
```

## 📚 Documentation

- [Installation Guide](docs/INSTALLATION.md) *(coming soon)*
- [Running the AppHost](docs/RUNNING_APPHOST.md)
- [Achievement System](docs/achievement-system.md)
- [Barcode Lookup](docs/BARCODE_LOOKUP.md)
- [Developer Guidelines](CLAUDE.md)

## 🏗️ Technology Stack

- **Backend**: .NET 10, ASP.NET Core Web API
- **Frontend**: Blazor (Server & MAUI Hybrid)
- **Database**: PostgreSQL with Entity Framework Core
- **Orchestration**: .NET Aspire
- **UI Framework**: Bootstrap 5
- **Authentication**: JWT tokens
- **Testing**: xUnit, bUnit, Playwright

## 🛠️ Building a Release

### Automated (GitHub Actions)
1. Create and push a version tag:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```
2. GitHub Actions will automatically build and create a release

### Manual Build
```bash
# Update version in Directory.Build.props
dotnet publish src/Snackbox.AppHost/Snackbox.AppHost.csproj -c Release -r win-x64 --self-contained

# Create release package
Compress-Archive -Path .\artifacts\* -DestinationPath snackbox-v1.0.0-win-x64.zip
```

## 🤝 Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/)
- UI powered by [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
- Database by [PostgreSQL](https://www.postgresql.org/)

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/YOUR_GITHUB_USERNAME/snackbox-claude/issues)
- **Discussions**: [GitHub Discussions](https://github.com/YOUR_GITHUB_USERNAME/snackbox-claude/discussions)

---

Made with ❤️ by the Snackbox Team
