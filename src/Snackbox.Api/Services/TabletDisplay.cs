using System.Diagnostics;
using System.Text.RegularExpressions;

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

    // `wm dismiss-keyguard` only exists from Android 8 - on older tablets (screen lock "None")
    // there is no keyguard to dismiss, so its failure is ignored. It runs in the background: like
    // `input`, it starts a Java process, which takes 2 seconds on a cheap tablet.
    // A browser page may only go full screen after a touch, and loses it when the screen goes off;
    // Firefox installs a page as an app (which would start full screen) only over HTTPS. So when
    // Android says the page is not full screen, one tap lets the page ask for it - the page shows
    // a welcome screen until then, so the tap hits nothing.
    // Fallback when the input devices are unknown: `input`, 2 seconds per call.
    private const string InputWakeCommand =
        "shell \"wm dismiss-keyguard >/dev/null 2>&1 & input keyevent KEYCODE_WAKEUP; sleep 1; " +
        "dumpsys window policy | grep -q mTopIsFullscreen=true || input tap 400 250; true\"";
    private const string InputSleepCommand = "shell input keyevent KEYCODE_SLEEP";

    /// <summary>The purchase is over: dark until the next scan.</summary>
    public void Sleep()
    {
        if (!configuration.GetValue("Tablet:WakeOnScan", true)) return;

        // The next scan must wake it again, however soon it comes
        lock (_lock) _lastWake = DateTime.MinValue;

        var adb = FindAdb();
        if (adb != null) _ = Task.Run(() => Run(adb, SleepCommand(adb)));
    }

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

        _ = Task.Run(() => Run(adb, WakeCommand(adb)));
    }

    private string WakeCommand(string adb)
    {
        var devices = _devices ??= FindInputDevices(adb);
        if (devices == null) return InputWakeCommand;

        // Straight into the kernel's input devices (`sendevent`, no Java start): the screen is on
        // and the page full screen within a second of the scan, before anyone touches it
        var wake = devices.PowerKey is { } key ? PowerKeyPress(key) : "input keyevent KEYCODE_WAKEUP";
        // Middle of the top middle compartment - the tablet's own cell, text only: harmless even if
        // the page got full screen in the meantime
        var (x, y) = (400 * devices.MaxX / devices.Width, 250 * devices.MaxY / devices.Height);
        var t = devices.Touch;
        var tap = $"sendevent {t} 3 57 0; sendevent {t} 3 53 {x}; sendevent {t} 3 54 {y}; sendevent {t} 3 48 5; " +
                  $"sendevent {t} 1 330 1; sendevent {t} 0 2 0; sendevent {t} 0 0 0; " +
                  $"sendevent {t} 1 330 0; sendevent {t} 0 2 0; sendevent {t} 0 0 0";

        // The power key toggles - only when asleep. Half a second for the page to come back
        return "shell \"wm dismiss-keyguard >/dev/null 2>&1 & " +
               $"dumpsys power | grep -q mWakefulness=Awake || {{ {wake}; sleep 0.5; }}; " +
               $"dumpsys window policy | grep -q mTopIsFullscreen=true || {{ {tap}; }}; true\"";
    }

    // The page has just put its welcome screen up - dark right away, not 2 seconds later
    private string SleepCommand(string adb) =>
        (_devices ??= FindInputDevices(adb))?.PowerKey is { } key
            ? $"shell \"dumpsys power | grep -q mWakefulness=Awake && {{ {PowerKeyPress(key)}; }}; true\""
            : InputSleepCommand;

    private static string PowerKeyPress(string key) =>
        $"sendevent {key} 1 116 1; sendevent {key} 0 0 0; sendevent {key} 1 116 0; sendevent {key} 0 0 0";

    private sealed record InputDevices(string Touch, int MaxX, int MaxY, int Width, int Height, string? PowerKey);

    private InputDevices? _devices;

    /// <summary>
    /// The touch screen and a power key, from `getevent -pl`. Only single-touch-report devices
    /// (multi-touch protocol A, no ABS_MT_SLOT) - the tap events are written for that protocol.
    /// </summary>
    // ponytail: protocol A and portrait only; protocol B (slots) or a rotated touch panel fall back to `input`
    private InputDevices? FindInputDevices(string adb)
    {
        var events = Output(adb, "shell getevent -pl");
        var size = Regex.Match(Output(adb, "shell wm size") ?? "", @"(\d+)x(\d+)\s*$");
        if (events == null || !size.Success) return null;

        string? touch = null, powerKey = null;
        int maxX = 0, maxY = 0;
        foreach (var block in events.Split("add device").Skip(1))
        {
            var device = Regex.Match(block, @"(/dev/input/event\d+)").Value;
            if (block.Contains("ABS_MT_POSITION_X") && !block.Contains("ABS_MT_SLOT") && touch == null)
            {
                touch = device;
                maxX = MaxOf(block, "ABS_MT_POSITION_X");
                maxY = MaxOf(block, "ABS_MT_POSITION_Y");
            }
            else if (block.Contains("KEY_POWER") && !block.Contains("ABS_") && powerKey == null)
            {
                powerKey = device;
            }
        }

        if (touch == null || maxX <= 0 || maxY <= 0) return null;
        logger.LogInformation("Tablet: touch screen {Touch}, power key {PowerKey}", touch, powerKey ?? "via input");
        return new InputDevices(touch, maxX, maxY, int.Parse(size.Groups[1].Value), int.Parse(size.Groups[2].Value), powerKey);

        static int MaxOf(string block, string axis)
        {
            var match = Regex.Match(block, axis + @"\s*:.*?max (\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }
    }

    private string? Output(string adb, string arguments)
    {
        try
        {
            // The adb server must already run: started from here it would inherit the output pipe
            // and keep it open
            Run(adb, "start-server");
            using var process = Process.Start(new ProcessStartInfo(adb, arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
            if (process == null) return null;
            var output = process.StandardOutput.ReadToEndAsync();
            if (!process.WaitForExit(TimeSpan.FromSeconds(10)))
            {
                process.Kill();
                return null;
            }
            return process.ExitCode == 0 ? output.Result : null;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            logger.LogWarning(ex, "Tablet: could not run adb");
            return null;
        }
    }

    private void Run(string adb, string arguments)
    {
        try
        {
            // No redirected output: if this call starts the adb server, the server would inherit
            // the pipes and keep them open
            using var process = Process.Start(new ProcessStartInfo(adb, arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false
            });
            if (process == null) return;
            if (!process.WaitForExit(TimeSpan.FromSeconds(10)))
            {
                process.Kill();
                logger.LogWarning("Tablet: adb {Arguments} timed out", arguments);
            }
            else if (process.ExitCode != 0)
            {
                // Usually the tablet is unplugged - worth a line, not an error
                logger.LogInformation("Tablet: adb {Arguments} failed (exit code {ExitCode})", arguments, process.ExitCode);
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
