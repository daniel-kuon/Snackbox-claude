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

A Snackbox installation is a **git checkout that is built and run in place** - the Aspire
AppHost orchestrates the API, the website and the containers, and the installed version is the
release tag that is checked out.

Needs [git](https://git-scm.com/download/win), the
[.NET 10 SDK](https://dotnet.microsoft.com/download) with the `aspire` and `maui` workloads, and
[Docker Desktop](https://www.docker.com/products/docker-desktop/). No administrator shell
required.

```powershell
irm https://raw.githubusercontent.com/daniel-kuon/Snackbox-claude/main/install-snackbox.ps1 | iex
```

The installer clones the repository, checks out the newest release tag, builds it in Release,
registers autostart at logon and starts everything.

To pass options - for example while the old Snackbox still owns the screen - download the
script first, since `irm | iex` cannot take parameters:

```powershell
irm https://raw.githubusercontent.com/daniel-kuon/Snackbox-claude/main/install-snackbox.ps1 -OutFile install-snackbox.ps1
.\install-snackbox.ps1 -InstallPath C:\Snackbox -NoKiosk
```

See the [Installation Guide](docs/INSTALLATION.md) for every option.

## 🔄 Updating

Open **Admin -> Updates** in Snackbox. It shows the installed version, the newest GitHub release
and its notes. Install stops the stack, moves the checkout to that tag, rebuilds and starts
again - a few minutes during which the kiosk is unavailable. If the build fails, the previous
commit is checked back out and restarted.

From the command line:

```powershell
& "C:\Snackbox\tools\Snackbox.Updater\bin\Release\net10.0\Snackbox.Updater.exe" update --tag v1.2.3
```

The whole run is written to `<install>\.snackbox\updater.log`.

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
git clone https://github.com/daniel-kuon/Snackbox-claude.git
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
├── tools/
│   └── Snackbox.Updater/          # Starts, stops and updates an installation
├── tests/
│   ├── Snackbox.Api.Tests/        # API unit tests
│   └── Snackbox.Components.Tests/ # Component tests (bUnit)
└── docs/                          # Documentation
```

## 📚 Documentation

- [Installation, autostart and updates](docs/INSTALLATION.md)
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

A release is a **tag plus notes** - there is nothing to ship as a binary, because the Aspire
AppHost cannot be published and moved (its generated project metadata hardcodes the build
machine's absolute `.csproj` paths).

Run the **Release** workflow in GitHub Actions and pick major / minor / bugfix, or push a tag:

```bash
git tag v1.0.0
git push origin v1.0.0
```

The workflow builds the solution with the same command an installation uses - if that fails,
the release would brick every machine that installs it - and then publishes the release that
the Updates page reads.

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

- **Issues**: [GitHub Issues](https://github.com/daniel-kuon/Snackbox-claude/issues)
- **Discussions**: [GitHub Discussions](https://github.com/daniel-kuon/Snackbox-claude/discussions)

---

Made with ❤️ by the Snackbox Team
