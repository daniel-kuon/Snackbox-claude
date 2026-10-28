using System.Diagnostics;

namespace Snackbox.Updater;

/// <summary>
/// Starts and stops what runs on the Snackbox machine: Docker Desktop (Postgres and SigNoz are
/// containers), the Aspire AppHost, and the kiosk window.
/// </summary>
public sealed class Stack(Installation installation, Log log)
{
    public Installation Installation => installation;

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
        // Two starts at once (two autostart entries, a click on start-snackbox.bat at logon)
        // opened two AppHosts and two kiosks. One at a time, and each skips what already runs.
        log.Write("Starting Snackbox...");
        using var single = new Mutex(false, @"Local\Snackbox.Updater.Start");
        var owned = false;
        try
        {
            try
            {
                owned = single.WaitOne(0);
                if (!owned)
                {
                    log.Write("Another start is under way - waiting for it to finish...");
                    owned = single.WaitOne(TimeSpan.FromMinutes(10));
                }
            }
            catch (AbandonedMutexException)
            {
                owned = true; // the other start died - carry on
            }

            Autostart.RemoveDuplicate(log);
            EnsureDockerRunning();
            if (IsRunning("Snackbox.AppHost")) log.Write("The AppHost is already running.");
            else StartAppHost();
            if (withKiosk) StartKiosk();
            Tablet.Connect(installation, log);
        }
        finally
        {
            if (owned) single.ReleaseMutex();
        }
    }

    private static bool IsRunning(string processName)
    {
        var processes = Process.GetProcessesByName(processName);
        foreach (var p in processes) p.Dispose();
        return processes.Length > 0;
    }

    public void Stop()
    {
        var watch = Stopwatch.StartNew();

        // Aspire's orchestrator (dcp.exe) outlives a killed AppHost and goes on shutting its
        // resources down for a while - long enough to take the next AppHost's API and website
        // with it, which left an installation without a backend after an update. It goes first:
        // killed after the AppHost it was already busy tearing down containers and processes,
        // and on the kiosk PC killing it then took 49 s. Each DCP is told which AppHost to watch
        // ("--monitor <pid>"), so only ours are stopped, not the DCP of some other Aspire app on
        // the machine. Found before anything is killed, while the AppHosts still exist to be
        // matched against.
        var orchestrators = OrchestratorsMonitoring(Process.GetProcessesByName("Snackbox.AppHost").Select(p => p.Id));

        var stopped = new List<Process>();
        foreach (var pid in orchestrators.Concat(installation.RememberedProcesses()))
        {
            KillTree(pid, stopped);
        }

        installation.ForgetProcesses();

        // Aspire's DCP starts the API and the website outside the AppHost's process tree, so
        // killing the "dotnet run" we remembered leaves them behind - and they hold the very
        // DLLs the rebuild has to replace. Anything still called Snackbox.* belongs to us.
        foreach (var name in StackProcessNames)
        {
            foreach (var process in Process.GetProcessesByName(name))
            {
                KillTree(process.Id, stopped);
            }
        }

        // Killing only asks Windows to end them; their files are free once they have exited
        var deadline = DateTime.UtcNow.AddSeconds(60);
        foreach (var process in stopped)
        {
            var left = deadline - DateTime.UtcNow;
            if (!process.WaitForExit(left > TimeSpan.Zero ? left : TimeSpan.Zero))
            {
                log.Write($"Process {process.Id} has not exited after 60 s.");
            }
            process.Dispose();
        }

        log.Write($"Stack stopped ({watch.Elapsed.TotalSeconds:0} s).");
    }

    private List<int> OrchestratorsMonitoring(IEnumerable<int> appHostPids)
    {
        var pids = string.Join("|", appHostPids);
        if (pids.Length == 0) return [];

        // No .NET API reads another process's command line; WMI via PowerShell does
        var script = "Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'dcp.exe' -and " +
                     $"$_.CommandLine -match '--monitor ({pids})( |$)' }} | ForEach-Object {{ $_.ProcessId }}";
        var output = Installation.Run("powershell", $"-NoProfile -Command \"{script}\"", installation.Root, throwOnError: false);

        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                     .Select(line => int.TryParse(line, out var pid) ? pid : 0)
                     .Where(pid => pid > 0)
                     .ToList();
    }

    /// <summary>
    /// In-process rather than taskkill, which on the kiosk PC sat for most of a minute on one
    /// process tree. Does not wait for the exit - <see cref="Stop"/> waits for all at once.
    /// </summary>
    private void KillTree(int pid, List<Process> stopped)
    {
        Process process;
        try
        {
            process = Process.GetProcessById(pid);
        }
        catch (ArgumentException)
        {
            return; // already gone, e.g. with the tree of a process stopped before
        }

        try
        {
            var name = process.ProcessName;
            process.Kill(entireProcessTree: true);
            stopped.Add(process);
            log.Write($"Stopped process {pid} ({name}).");
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or AggregateException)
        {
            // A process that is ending already refuses to be killed again
            if (process.HasExited)
            {
                process.Dispose();
                return;
            }

            stopped.Add(process);
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

        // "Installed" runs the stack as Production - the default profile is for development and
        // would switch the API's test helper (password bypass, database reset) on.
        // The AppHost outlives this updater, so it must not write into a pipe we own: once we
        // exit, its next log line hits a closed pipe. cmd hands it a file to write to instead.
        var output = Path.Combine(installation.StateDirectory, "apphost.log");
        var info = new ProcessStartInfo("cmd.exe",
                                        $"/c dotnet run --project src/Snackbox.AppHost -c Release --no-build --launch-profile Installed > \"{output}\" 2>&1")
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

    public void StartKiosk()
    {
        var kiosk = Path.Combine(installation.Root, "src", "Snackbox.Web", "bin", "Release",
                                 KioskFramework, "win-x64", "Snackbox.Web.exe");

        if (IsRunning("Snackbox.Web"))
        {
            log.Write("The kiosk is already running.");
            return;
        }

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
