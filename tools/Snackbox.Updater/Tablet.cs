using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Snackbox.Updater;

/// <summary>
/// The Android tablet hangs on a USB cable and has no network of its own: `adb reverse` makes
/// its 127.0.0.1:5186 the PC's website, where it shows /tablet. The forwarding is lost whenever
/// the cable, the tablet or the adb server restarts, so it is set again on every start and every
/// watchdog run - it is idempotent and quick. Silent when there is no adb or no tablet.
///
/// The tablet is also pinned to the page (`am task lock` = Android's screen pinning, asked for from
/// the PC): Home, the app switcher and the notification shade do nothing, so nobody leaves the page
/// by accident. Android asks once on the tablet to confirm it; holding Back + app switcher ends it
/// on purpose, and so does a restart of the tablet - the next watchdog run asks again. A full lock
/// would need a device-owner app, and was not wanted. `--unlock` unpins and leaves a marker so
/// the watchdog keeps its hands off until `tablet --lock`.
/// </summary>
public static class Tablet
{
    private const string WebsitePort = "5186";
    private const string Browser = "org.mozilla.firefox";
    private static readonly string PageUrl = $"http://127.0.0.1:{WebsitePort}/tablet";

    public static void Connect(Installation installation, Log log, bool verbose = false)
    {
        var adb = FindAdb();
        if (adb == null)
        {
            if (verbose) log.Write("Tablet: adb.exe not found (put it on PATH or set SNACKBOX_ADB).");
            return;
        }

        try
        {
            // The adb server first, without redirected output: started by any other command it
            // would inherit our pipes and keep them open forever, so reading them would never end
            using (var server = Process.Start(new ProcessStartInfo(adb, "start-server") { CreateNoWindow = true, UseShellExecute = false }))
            {
                server?.WaitForExit(TimeSpan.FromSeconds(30));
            }

            var (ok, _, error) = Run(adb, $"reverse tcp:{WebsitePort} tcp:{WebsitePort}");
            if (!ok)
            {
                // Typically "no devices/emulators found": unplugged, or USB debugging not allowed yet
                if (verbose) log.Write($"Tablet: adb reverse failed - {error}");
                return;
            }
            if (verbose) log.Write($"Tablet: {PageUrl} on the tablet now reaches the website.");

            if (!File.Exists(UnlockedMarker(installation))) LockToPage(adb, log, verbose);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            if (verbose) log.Write($"Tablet: could not run adb - {ex.Message}");
        }
    }

    /// <summary>`tablet --unlock`: free the tablet (maintenance) and keep the watchdog from locking it again.</summary>
    public static void Unlock(Installation installation, Log log)
    {
        installation.EnsureStateDirectory();
        File.WriteAllText(UnlockedMarker(installation), "Remove this file or run `tablet --lock` to lock the tablet to the Snackbox page again.");

        var adb = FindAdb();
        var (ok, _, error) = adb == null ? (false, "", "adb.exe not found") : Run(adb, "shell am task lock stop");
        log.Write(ok ? "Tablet: unlocked; the watchdog leaves it unlocked until `tablet --lock`." : $"Tablet: unlock failed - {error}");
    }

    /// <summary>`tablet --lock`: back to the kiosk lock (also clears the marker of --unlock).</summary>
    public static void Lock(Installation installation, Log log)
    {
        File.Delete(UnlockedMarker(installation));
        Connect(installation, log, verbose: true);
    }

    private static string UnlockedMarker(Installation installation) => Path.Combine(installation.StateDirectory, "tablet-unlocked");

    private static void LockToPage(string adb, Log log, bool verbose)
    {
        // Locked already (a lock does not survive a restart of the tablet) - leave the page alone
        var (_, state, _) = Run(adb, "shell dumpsys activity activities");
        if (Regex.IsMatch(state, @"mLockTaskModeState=(LOCKED|PINNED)")) return;

        var (installed, _, _) = Run(adb, $"shell pm path {Browser}");
        if (!installed)
        {
            if (verbose) log.Write($"Tablet: {Browser} is not installed - not locking the tablet to the page.");
            return;
        }

        Run(adb, $"shell input keyevent KEYCODE_WAKEUP");
        Run(adb, $"shell am start -a android.intent.action.VIEW -d {PageUrl} {Browser}");
        Thread.Sleep(TimeSpan.FromSeconds(3));

        // The browser's task: "taskId=17: org.mozilla.firefox/... visible=true"
        var (_, stacks, _) = Run(adb, "shell am stack list");
        var task = Regex.Match(stacks, $@"taskId=(\d+): {Regex.Escape(Browser)}/");
        if (!task.Success)
        {
            log.Write("Tablet: the browser's task was not found - not locked.");
            return;
        }

        var (ok, _, error) = Run(adb, $"shell am task lock {task.Groups[1].Value}");
        if (!ok)
        {
            log.Write($"Tablet: pinning failed - {error}");
            return;
        }

        // Android asks on the tablet first; the command returns before anyone answered
        Thread.Sleep(TimeSpan.FromSeconds(2));
        var (_, after, _) = Run(adb, "shell dumpsys activity activities");
        log.Write(Regex.IsMatch(after, @"mLockTaskModeState=(LOCKED|PINNED)")
            ? "Tablet: pinned to the Snackbox page."
            : "Tablet: asked to pin the Snackbox page - confirm it on the tablet.");
    }

    private static (bool Ok, string Output, string Error) Run(string adb, string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo(adb, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            UseShellExecute = false
        });
        if (process == null) return (false, "", "adb did not start");

        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        if (!process.WaitForExit(TimeSpan.FromSeconds(30))) process.Kill();

        // adb shell passes the device command's exit code through; "pm path" of a missing package prints nothing
        var ok = process.ExitCode == 0 && !(arguments.Contains("pm path") && string.IsNullOrWhiteSpace(output));
        return (ok, output, error.Result.Trim());
    }

    private static string? FindAdb()
    {
        var configured = Environment.GetEnvironmentVariable("SNACKBOX_ADB");
        if (!string.IsNullOrWhiteSpace(configured)) return File.Exists(configured) ? configured : null;

        var candidates = (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(dir => Path.Combine(dir, "adb.exe"))
            .Append(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Android\Sdk\platform-tools\adb.exe"))
            .Append(@"C:\platform-tools\adb.exe");

        return candidates.FirstOrDefault(File.Exists);
    }
}
