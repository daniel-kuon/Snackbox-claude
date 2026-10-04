namespace Snackbox.Updater;

/// <summary>
/// Autostart is a scheduled task that runs at logon, not a service: Docker Desktop and the
/// kiosk window both need a desktop session, which a service does not have.
/// </summary>
public static class Autostart
{
    public const string TaskName = "Snackbox";

    public static void Install(Installation installation, bool withKiosk, Log log)
    {
        var updater = Environment.ProcessPath
                      ?? throw new UpdaterException("Could not determine the path of the updater itself.");

        var arguments = $"start --dir \"{installation.Root}\"" + (withKiosk ? "" : " --no-kiosk");
        var command = $"\\\"{updater}\\\" {arguments}";

        // /f replaces an existing task, so re-running this is how you change the options.
        Installation.Run("schtasks",
                         $"/create /tn \"{TaskName}\" /tr \"{command}\" /sc onlogon /f",
                         installation.Root);

        log.Write($"Autostart registered as the scheduled task \"{TaskName}\"{(withKiosk ? "" : " (without the kiosk)")}.");
        log.Write($"It runs: {updater} {arguments}");
    }

    public static void Uninstall(Log log)
    {
        Installation.Run("schtasks", $"/delete /tn \"{TaskName}\" /f", Environment.CurrentDirectory, throwOnError: false);
        log.Write($"Autostart task \"{TaskName}\" removed.");
    }
}
