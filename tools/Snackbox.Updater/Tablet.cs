using System.Diagnostics;

namespace Snackbox.Updater;

/// <summary>
/// The Android tablet hangs on a USB cable and has no network of its own: `adb reverse` makes
/// its localhost:5186 the PC's website, where it shows /tablet. The forwarding is lost whenever
/// the cable, the tablet or the adb server restarts, so it is set again on every start and every
/// watchdog run - it is idempotent and quick. Silent when there is no adb or no tablet.
/// </summary>
public static class Tablet
{
    private const string WebsitePort = "5186";

    public static void Connect(Log log, bool verbose = false)
    {
        var adb = FindAdb();
        if (adb == null)
        {
            if (verbose) log.Write("Tablet: adb.exe not found (put it on PATH or set SNACKBOX_ADB).");
            return;
        }

        try
        {
            // The adb server first, without redirected output: started by "reverse" it would
            // inherit our pipes and keep them open forever, so reading them would never end
            using (var server = Process.Start(new ProcessStartInfo(adb, "start-server") { CreateNoWindow = true, UseShellExecute = false }))
            {
                server?.WaitForExit(TimeSpan.FromSeconds(30));
            }

            using var process = Process.Start(new ProcessStartInfo(adb, $"reverse tcp:{WebsitePort} tcp:{WebsitePort}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            });
            if (process == null) return;

            var error = process.StandardError.ReadToEndAsync();
            process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(TimeSpan.FromSeconds(30))) process.Kill();

            if (process.ExitCode == 0)
            {
                if (verbose) log.Write($"Tablet: http://127.0.0.1:{WebsitePort}/tablet on the tablet now reaches the website.");
            }
            else if (verbose)
            {
                // Typically "no devices/emulators found": unplugged, or USB debugging not allowed yet
                log.Write($"Tablet: adb reverse failed - {error.Result.Trim()}");
            }
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            if (verbose) log.Write($"Tablet: could not run adb - {ex.Message}");
        }
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
