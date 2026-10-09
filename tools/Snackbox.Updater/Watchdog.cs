using System.Diagnostics;

namespace Snackbox.Updater;

/// <summary>
/// Meant to run every few minutes from a scheduled task (see <see cref="InstallTask"/>): if the
/// API or the website stop answering, the whole stack is restarted; if only the kiosk window is
/// gone, just the kiosk. Quiet while everything is fine - it only writes to the log when it acts.
/// </summary>
public sealed class Watchdog(Stack stack, Log log)
{
    public const string TaskName = "Snackbox Watchdog";

    // The ports of the AppHost's "Installed" launch profile
    private static readonly string[] Endpoints = ["http://localhost:5057/health", "http://localhost:5186/login"];

    /// <summary>A freshly started stack needs a while before it answers; leave it alone meanwhile.</summary>
    private static readonly TimeSpan StartupGrace = TimeSpan.FromMinutes(3);

    public int Run(bool withKiosk)
    {
        // An update or a start is in progress - both stop or start the stack themselves
        if (OtherUpdatersRunning()) return 0;

        var appHost = Process.GetProcessesByName("Snackbox.AppHost").FirstOrDefault();
        if (appHost != null && DateTime.Now - appHost.StartTime < StartupGrace) return 0;

        if (appHost != null && StackAnswers())
        {
            if (withKiosk && Process.GetProcessesByName("Snackbox.Web").Length == 0)
            {
                log.Write("Watchdog: the kiosk is not running - starting it.");
                stack.StartKiosk();
            }

            return 0;
        }

        // Checked again: the health checks took a while, and an update may have begun meanwhile
        if (OtherUpdatersRunning()) return 0;

        log.Write($"Watchdog: Snackbox is not answering{(appHost == null ? " (no AppHost running)" : "")} - restarting it.");
        stack.Stop();
        stack.Start(withKiosk);
        return 0;
    }

    /// <summary>Three tries, ten seconds apart: one slow answer is not worth a restart.</summary>
    private static bool StackAnswers()
    {
        // No redirects: the website may redirect to HTTPS with a self-signed certificate, and
        // any answer at all proves it is alive.
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            if (Endpoints.All(url => Answers(http, url))) return true;
            if (attempt < 3) Thread.Sleep(TimeSpan.FromSeconds(10));
        }

        return false;
    }

    private static bool Answers(HttpClient http, string url)
    {
        try
        {
            using var response = http.GetAsync(url).GetAwaiter().GetResult();
            return (int)response.StatusCode < 500;
        }
        catch
        {
            return false;
        }
    }

    public static bool OtherUpdatersRunning() =>
        Process.GetProcessesByName("Snackbox.Updater").Any(p => p.Id != Environment.ProcessId);

    /// <summary>Lets a watchdog run that is already under way finish before an update stops the stack.</summary>
    public static void WaitForOtherUpdaters(Log log)
    {
        var deadline = DateTime.UtcNow.AddSeconds(90);
        if (!OtherUpdatersRunning()) return;

        log.Write("Waiting for another updater (probably the watchdog) to finish...");
        while (OtherUpdatersRunning() && DateTime.UtcNow < deadline)
        {
            Thread.Sleep(TimeSpan.FromSeconds(2));
        }
    }

    /// <summary>
    /// Every 5 minutes for the logged-in user. Unlike the logon trigger of the autostart, a
    /// time trigger for one's own account needs no administrator rights. Started through
    /// "conhost --headless" so no console window flashes up - that would take the focus from
    /// the kiosk and drop it out of fullscreen every five minutes.
    /// </summary>
    public static void InstallTask(Installation installation, bool withKiosk, Log log)
    {
        var updater = Environment.ProcessPath
                      ?? throw new UpdaterException("Could not determine the path of the updater itself.");

        var arguments = $"watchdog --dir \\\"{installation.Root}\\\"" + (withKiosk ? "" : " --no-kiosk");
        Installation.Run("schtasks",
                         $"/create /tn \"{TaskName}\" /tr \"conhost.exe --headless \\\"{updater}\\\" {arguments}\" /sc minute /mo 5 /f",
                         installation.Root);

        log.Write($"Watchdog registered as the scheduled task \"{TaskName}\" (every 5 minutes).");
    }

    public static void UninstallTask(Log log)
    {
        Installation.Run("schtasks", $"/delete /tn \"{TaskName}\" /f", Environment.CurrentDirectory, throwOnError: false);
        log.Write("Watchdog task removed.");
    }
}
