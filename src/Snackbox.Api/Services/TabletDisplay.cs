using System.Diagnostics;

namespace Snackbox.Api.Services;

/// <summary>
/// Wakes the USB tablet's screen when a card is scanned, so it can sleep in between. Done over
/// the USB debugging connection the tablet already has for `adb reverse` - no app on the tablet:
/// `input keyevent KEYCODE_WAKEUP` turns the screen on (and is a no-op when it is on), `wm
/// dismiss-keyguard` gets past a swipe lock screen. Fire-and-forget: a scan never waits for it.
/// </summary>
public class TabletDisplay(IConfiguration configuration, ILogger<TabletDisplay> logger)
{
    private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(5);
    private readonly Lock _lock = new();
    private DateTime _lastWake = DateTime.MinValue;
    private bool _warnedNoAdb;

    public void Wake()
    {
        if (!configuration.GetValue("Tablet:WakeOnScan", true)) return;

        // A burst of scans needs one wake-up, not one adb call each
        lock (_lock)
        {
            if (DateTime.UtcNow - _lastWake < MinInterval) return;
            _lastWake = DateTime.UtcNow;
        }

        var adb = FindAdb();
        if (adb == null)
        {
            if (!_warnedNoAdb) logger.LogInformation("Tablet: adb.exe not found - the tablet's screen is not woken on scans");
            _warnedNoAdb = true;
            return;
        }

        _ = Task.Run(() => Run(adb));
    }

    private void Run(string adb)
    {
        try
        {
            // No redirected output: if this call starts the adb server, the server would inherit
            // the pipes and keep them open
            using var process = Process.Start(new ProcessStartInfo(adb, "shell \"input keyevent KEYCODE_WAKEUP && wm dismiss-keyguard\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            });
            if (process == null) return;
            if (!process.WaitForExit(TimeSpan.FromSeconds(10)))
            {
                process.Kill();
                logger.LogWarning("Tablet: waking the screen timed out");
            }
            else if (process.ExitCode != 0)
            {
                // Usually the tablet is unplugged - worth a line, not an error
                logger.LogInformation("Tablet: adb could not wake the screen (exit code {ExitCode})", process.ExitCode);
            }
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            logger.LogWarning(ex, "Tablet: could not run adb");
        }
    }

    // Same places the updater's `tablet` command looks
    private string? FindAdb()
    {
        var configured = configuration["Tablet:AdbPath"] ?? Environment.GetEnvironmentVariable("SNACKBOX_ADB");
        if (!string.IsNullOrWhiteSpace(configured)) return File.Exists(configured) ? configured : null;

        return (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(dir => Path.Combine(dir, "adb.exe"))
            .Append(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Android\Sdk\platform-tools\adb.exe"))
            .Append(@"C:\platform-tools\adb.exe")
            .FirstOrDefault(File.Exists);
    }
}
