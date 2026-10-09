# Installing and updating Snackbox

A Snackbox installation is a **git checkout that is built and run in place**. The installed
version is the release tag that is checked out, so there is no version file to keep in sync and
rolling back is just checking out the previous tag.

## Requirements

- Windows 10/11 (x64)
- [git](https://git-scm.com/download/win)
- [.NET 10 SDK](https://dotnet.microsoft.com/download) with the `aspire` and `maui` workloads
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — Postgres and SigNoz run as
  containers started by the Aspire AppHost

```powershell
dotnet workload install aspire
dotnet workload install maui
```

## Install

```powershell
irm https://raw.githubusercontent.com/daniel-kuon/Snackbox-claude/main/install-snackbox.ps1 | iex
```

or, with options:

```powershell
.\install-snackbox.ps1 -InstallPath C:\Snackbox -NoKiosk
```

The script clones the repository, checks out the newest `v*.*.*` tag, builds the solution in
Release, registers autostart and starts everything.

| Option | Effect |
| --- | --- |
| `-InstallPath` | Where to install. Default `C:\Snackbox` |
| `-NoKiosk` | Autostart brings up Docker and the backend but not the kiosk window — useful while the old Snackbox still owns the screen |
| `-NoStart` | Install and build only; register nothing and start nothing |

## Autostart

Autostart runs **at logon**, not as a Windows service: Docker Desktop and the kiosk window both
need a desktop session, which a service does not have. It runs `Snackbox.Updater start`, which
waits for Docker to answer `docker info`, starts the Aspire AppHost and then launches the kiosk.

A scheduled task is used when possible. Creating a logon-triggered task needs an elevated
shell, so without one the updater falls back to `Snackbox.cmd` in the user's Startup folder -
same effect for the logged-in user, no administrator needed. The log says which one was used,
and `uninstall-autostart` removes both.

```powershell
$updater = "C:\Snackbox\tools\Snackbox.Updater\bin\Release\net10.0\Snackbox.Updater.exe"

& $updater install-autostart            # register (re-run to change options)
& $updater install-autostart --no-kiosk # backend only
& $updater uninstall-autostart          # remove
```

## Watchdog

`Snackbox.Updater watchdog` checks that the API (`http://localhost:5057/health`) and the
website (`http://localhost:5186/login`) answer - three tries, ten seconds apart. If they do not,
it stops and restarts the whole stack; if only the kiosk window is gone, it starts the kiosk. It
leaves the stack alone during the first 3 minutes after a start and while another updater runs
(an update or a start), and it only writes to `updater.log` when it acts.

Run it from a scheduled task every 5 minutes. `install-watchdog` registers exactly that for the
logged-in user (a time trigger needs no administrator rights). It runs through
`conhost.exe --headless`, so no console window flashes up and takes the focus from the kiosk:

```powershell
& $updater install-watchdog             # every 5 minutes (re-run to change options)
& $updater install-watchdog --no-kiosk  # don't start the kiosk window
& $updater uninstall-watchdog           # remove
```

To create the task by hand instead (Task Scheduler, "Run only when user is logged on"):

```
Program:   conhost.exe
Arguments: --headless "C:\Snackbox\tools\Snackbox.Updater\bin\Release\net10.0\Snackbox.Updater.exe" watchdog --dir "C:\Snackbox"
Trigger:   daily, repeat every 5 minutes indefinitely
```

## Updating

Open **Admin → Updates** in Snackbox. It shows the installed version, the newest GitHub release
and its notes. Install stops the stack, moves the checkout to the tag, rebuilds and starts
again — a few minutes during which the kiosk is unavailable.

Pressing Install opens a **terminal window on the Snackbox PC** that shows the update step by
step — the web UI and the kiosk are down while it runs, so that window is where to watch it. It
stays open for a minute after a successful update, and until you press Enter after a failed one.

From the command line:

```powershell
& $updater update --tag v1.2.3
& $updater update --tag v1.2.3 --no-start   # update but leave it stopped
```

**If the build fails**, the updater checks the previous commit back out, rebuilds and starts it
again, so a bad release cannot leave the machine dead. It refuses to run at all while the
checkout has uncommitted local changes.

The whole run is written to `<install>\.snackbox\updater.log`. That file is the only place the
outcome can be seen, because the update stops the very app that triggered it — the Updates page
reads it back once the app returns.

## Where the data lives

Nothing that matters lives inside the installation folder, so updating, moving or reinstalling
it cannot lose data:

| What | Where |
| --- | --- |
| Database | Docker volume `snackbox-postgres` (container `snackbox-postgres`, Postgres 18) |
| Backups | `%ProgramData%\Snackbox\backups` (`Backup:Directory` to change it) |
| Telemetry (SigNoz) | Docker volumes `signoz-clickhouse`, `signoz-sqlite`, `signoz-zookeeper-1` |

The Postgres major version is pinned on purpose. A new major version cannot open the old
one's data files, so a major upgrade means a backup and restore (Admin -> Backups), not
changing the image. Backups run inside the database container, so they need no PostgreSQL
install on the machine and always match the server's version.

## Other commands

```powershell
& $updater status   # installed version, commit, what is running
& $updater start    # Docker, then the Aspire stack, then the kiosk
& $updater stop     # stop everything the updater started
```

Every command takes `--dir <path>`; it otherwise falls back to `SNACKBOX_HOME` and then to the
repository the updater itself sits in.

## Releasing

Run the **Release** workflow in GitHub Actions (major / minor / bugfix), or push a `v*.*.*` tag.
The workflow builds the solution with the same command an installation uses — if that fails,
the release would brick every machine that installs it — and then publishes the release the
Updates page reads.
