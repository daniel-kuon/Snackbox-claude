namespace Snackbox.Updater;

/// <summary>
/// Moves the installation to a release tag: stop, check out, build, start. If the build fails
/// the previous commit is restored and started again, so a bad release cannot leave the snack
/// machine dead.
/// </summary>
public sealed class UpdateCommand(Installation installation, Stack stack, Log log)
{
    public int Run(string tag, bool withKiosk, bool start = true)
    {
        var previousCommit = installation.CurrentCommit();
        log.Write($"Updating {installation.Root} from {installation.CurrentVersion()} ({previousCommit[..7]}) to {tag}.");

        // Fetch before stopping anything: a network problem should not cost any downtime.
        log.Write("Fetching from origin...");
        installation.Git("fetch --tags --prune origin");

        var target = installation.Git($"rev-parse --verify --quiet refs/tags/{tag}^{{commit}}", throwOnError: false).Trim();
        if (string.IsNullOrEmpty(target))
        {
            throw new UpdaterException($"The repository has no tag {tag} after fetching. Is the release published?");
        }

        if (target == previousCommit)
        {
            log.Write("Already on that release - nothing to do.");
            return 0;
        }

        var dirty = installation.Git("status --porcelain", throwOnError: false).Trim();
        if (dirty.Length > 0)
        {
            throw new UpdaterException(
                "The installation has local changes. Commit or discard them before updating:" +
                Environment.NewLine + dirty);
        }

        stack.Stop();

        try
        {
            log.Write($"Checking out {tag}...");
            installation.Git($"-c advice.detachedHead=false checkout --force {target}");

            Build();
        }
        catch (Exception ex)
        {
            log.Write($"Update to {tag} failed: {ex.Message}");
            log.Write($"Rolling back to {previousCommit[..7]}...");

            try
            {
                installation.Git($"-c advice.detachedHead=false checkout --force {previousCommit}");
                Build();
                if (start) stack.Start(withKiosk);
                log.Write("Rolled back to the previous version.");
            }
            catch (Exception rollbackFailure)
            {
                log.Write($"ROLLBACK FAILED: {rollbackFailure.Message}");
                log.Write("The machine needs a hand - the checkout is not in a runnable state.");
            }

            return 1;
        }

        if (!start)
        {
            log.Write($"Updated to {installation.CurrentVersion()}; not starting (--no-start).");
            return 0;
        }

        stack.Start(withKiosk);
        log.Write($"Updated to {installation.CurrentVersion()} and started.");
        return 0;
    }

    private void Build()
    {
        log.Write("Restoring packages...");
        log.WriteRaw(Installation.Run("dotnet", "restore Snackbox.sln", installation.Root));

        log.Write("Building (Release)... this takes a few minutes.");
        log.WriteRaw(Installation.Run("dotnet", "build Snackbox.sln -c Release --no-restore", installation.Root));

        log.Write("Build finished.");
    }
}
