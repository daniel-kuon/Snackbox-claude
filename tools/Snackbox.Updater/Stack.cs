using System.Diagnostics;

namespace Snackbox.Updater;

/// <summary>
/// Starts and stops what runs on the Snackbox machine: Docker Desktop (Postgres and SigNoz are
/// containers), the Aspire AppHost, and the kiosk window.
/// </summary>
public sealed class Stack(Installation installation, Log log)
{
    private const string KioskFramework = "net10.0-windows10.0.19041.0";

    /// <summary>
    /// Everything an installation runs, by process name. The old Snackbox is "Snackboxx" with
    /// two x - deliberately not in this list, the updater has no business stopping it.
    /// </summary>
    private static readonly string[] StackProcessNames =
    [
        "Snackbox.AppHost",
        "Snackbox.Api",
        "Snackbox.BlazorServer",
        "Snackbox.Web"
    ];

    public void Start(bool withKiosk)
    {
        EnsureDockerRunning();
        StartAppHost();
        if (withKiosk) StartKiosk();
    }

    public void Stop()
    {
        foreach (var pid in installation.RememberedProcesses())
        {
            KillTree(pid);
        }

        installation.ForgetProcesses();

        // Aspire's DCP starts the API and the website outside the AppHost's process tree, so
        // killing the "dotnet run" we remembered leaves them behind - and they hold the very
        // DLLs the rebuild has to replace. Anything still called Snackbox.* belongs to us.
        foreach (var name in StackProcessNames)
        {
            foreach (var process in Process.GetProcessesByName(name))
            {
                KillTree(process.Id);
            }
        }

        log.Write("Stack stopped.");
    }

    private void KillTree(int pid)
    {
        try
        {
            Installation.Run("taskkill", $"/PID {pid} /T /F", installation.Root, throwOnError: false);
            log.Write($"Stopped process {pid}.");
        }
        catch (Exception ex)
        {
            log.Write($"Could not stop process {pid}: {ex.Message}");
        }
    }

    // --------------------------------------------------------------- docker

    /// <summary>
    /// Containers are started by the AppHost, so Docker has to be up first. On a fresh logon
    /// Docker Desktop takes a while, which is exactly when autostart runs.
    /// </summary>
    public void EnsureDockerRunning(TimeSpan? timeout = null)
    {
        if (DockerIsReady())
        {
            log.Write("Docker is already running.");
            return;
        }

        var executable = DockerDesktopPath();
        if (executable == null)
        {
            throw new UpdaterException("Docker is not running and Docker Desktop was not found. Start Docker manually.");
        }

        log.Write("Starting Docker Desktop...");
        Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true });

        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromMinutes(5));
        while (DateTime.UtcNow < deadline)
        {
            Thread.Sleep(TimeSpan.FromSeconds(5));
            if (!DockerIsReady()) continue;

            log.Write("Docker is ready.");
            return;
        }

        throw new UpdaterException("Docker Desktop did not become ready in time.");
    }

    private bool DockerIsReady()
    {
        try
        {
            return Installation.Run("docker", "info --format {{.ServerVersion}}", installation.Root, throwOnError: false).Trim().Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private static string? DockerDesktopPath()
    {
        string[] candidates =
        [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Docker", "Docker", "Docker Desktop.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Docker", "Docker Desktop.exe")
        ];

        return candidates.FirstOrDefault(File.Exists);
    }

    // -------------------------------------------------------------- aspire

    private void StartAppHost()
    {
        log.Write("Starting the Aspire AppHost...");

        // The AppHost outlives this updater, so it must not write into a pipe we own: once we
        // exit, its next log line hits a closed pipe. cmd hands it a file to write to instead.
        var output = Path.Combine(installation.StateDirectory, "apphost.log");
        var info = new ProcessStartInfo("cmd.exe",
                                        $"/c dotnet run --project src/Snackbox.AppHost -c Release --no-build > \"{output}\" 2>&1")
        {
            WorkingDirectory = installation.Root,
            // ShellExecute rather than CreateProcess: the AppHost must not inherit our console
            // or output handles, or whoever started us (a script, a pipe) waits for it forever.
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        var process = Process.Start(info) ?? throw new UpdaterException("Could not start the AppHost.");
        installation.RememberProcess(process.Id);

        log.Write($"AppHost started (pid {process.Id}), output in {output}.");
    }

    private void StartKiosk()
    {
        var kiosk = Path.Combine(installation.Root, "src", "Snackbox.Web", "bin", "Release",
                                 KioskFramework, "win-x64", "Snackbox.Web.exe");

        if (!File.Exists(kiosk))
        {
            log.Write($"Kiosk not found at {kiosk} - skipping. Build the solution in Release first.");
            return;
        }

        // The kiosk retries until the API answers, so there is no need to wait for it here.
        var process = Process.Start(new ProcessStartInfo(kiosk) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(kiosk)! });
        if (process != null)
        {
            installation.RememberProcess(process.Id);
            log.Write($"Kiosk started (pid {process.Id}).");
        }
    }
}
