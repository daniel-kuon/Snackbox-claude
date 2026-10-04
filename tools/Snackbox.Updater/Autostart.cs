namespace Snackbox.Updater;

/// <summary>
/// Autostart runs at logon rather than as a service: Docker Desktop and the kiosk window both
/// need a desktop session, which a service does not have.
///
/// A scheduled task is the nicer mechanism, but creating a logon-triggered one needs an
/// elevated shell ("FEHLER: Zugriff verweigert"). Rather than force the installer to run as
/// administrator, fall back to a command file in the user's Startup folder, which does the
/// same job for the logged-in user and needs no rights at all.
/// </summary>
public static class Autostart
{
    public const string TaskName = "Snackbox";

    private static string StartupCommandFile => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Snackbox.cmd");

    public static void Install(Installation installation, bool withKiosk, Log log)
    {
        var updater = Environment.ProcessPath
                      ?? throw new UpdaterException("Could not determine the path of the updater itself.");

        var arguments = $"start --dir \"{installation.Root}\"" + (withKiosk ? "" : " --no-kiosk");

        if (TryInstallScheduledTask(installation, updater, arguments, log))
        {
            // Both would start Snackbox twice.
            RemoveStartupCommand();
            log.Write($"Autostart registered as the scheduled task \"{TaskName}\"{(withKiosk ? "" : " (without the kiosk)")}.");
        }
        else
        {
            File.WriteAllText(StartupCommandFile,
                              $"@echo off{Environment.NewLine}start \"\" \"{updater}\" {arguments}{Environment.NewLine}");
            log.Write($"No rights for a scheduled task, so autostart is {StartupCommandFile} instead" +
                      $"{(withKiosk ? "" : " (without the kiosk)")}.");
        }

        log.Write($"It runs: {updater} {arguments}");
    }

    public static void Uninstall(Log log)
    {
        Installation.Run("schtasks", $"/delete /tn \"{TaskName}\" /f", Environment.CurrentDirectory, throwOnError: false);
        RemoveStartupCommand();
        log.Write("Autostart removed.");
    }

    private static bool TryInstallScheduledTask(Installation installation, string updater, string arguments, Log log)
    {
        try
        {
            // /f replaces an existing task, so re-running this is how the options are changed.
            Installation.Run("schtasks",
                             $"/create /tn \"{TaskName}\" /tr \"\\\"{updater}\\\" {arguments}\" /sc onlogon /f",
                             installation.Root);
            return true;
        }
        catch (UpdaterException ex)
        {
            log.Write($"Could not create the scheduled task ({ex.Message.Split('\n')[0].Trim()}).");
            return false;
        }
    }

    private static void RemoveStartupCommand()
    {
        try
        {
            if (File.Exists(StartupCommandFile)) File.Delete(StartupCommandFile);
        }
        catch
        {
            // Nothing useful to do; the next install overwrites it anyway.
        }
    }
}
