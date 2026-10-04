using System.Diagnostics;

namespace Snackbox.Updater;

/// <summary>
/// An update rebuilds the solution, and the solution contains the updater. Windows will not let
/// the build overwrite a running executable, so the updater first copies itself into the temp
/// folder and continues from there, leaving its own file in the installation free.
/// </summary>
public static class SelfCopy
{
    /// <summary>Internal marker telling the copy not to copy itself again. Not a user flag.</summary>
    public const string Marker = "snackbox-internal-from-temp-copy";

    /// <summary>
    /// Relaunches from a temp copy and returns true when the caller should stop. Returns false
    /// if we are already running from a copy, or if copying failed - the update is more
    /// important than updating the updater, so a failure here is not fatal.
    /// </summary>
    public static bool Relaunch(string[] args, Log log)
    {
        var source = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        if (source.StartsWith(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var target = Path.Combine(Path.GetTempPath(), $"snackbox-updater-{DateTime.Now:yyyyMMdd-HHmmss}");
            Directory.CreateDirectory(target);

            foreach (var file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
            }

            var executable = Path.Combine(target, Path.GetFileName(Environment.ProcessPath ?? "Snackbox.Updater.exe"));
            if (!File.Exists(executable)) return false;

            var info = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true };
            foreach (var argument in args) info.ArgumentList.Add(argument);
            info.ArgumentList.Add($"--{Marker}");

            // The copy has to outlive us: this process is the installed executable the build
            // is about to overwrite, so it cannot stay around to wait for the result.
            log.Write($"Continuing in the background from {executable} so the build can replace the installed updater.");
            log.Write($"Follow the update in {log.Path}");
            Process.Start(info);
            return true;
        }
        catch (Exception ex)
        {
            log.Write($"Could not run from a temp copy ({ex.Message}); continuing in place.");
            return false;
        }
    }
}
