using System.Diagnostics;
using System.Text;

namespace Snackbox.Updater;

/// <summary>
/// A Snackbox installation: a git checkout of the repository that is built and run in place.
/// The installed version is the tag that is checked out, so there is no version file to keep
/// in sync with the releases.
/// </summary>
public sealed class Installation
{
    public string Root { get; }

    private Installation(string root) => Root = root;

    public string StateDirectory => Path.Combine(Root, ".snackbox");
    public string PidFile => Path.Combine(StateDirectory, "running.txt");
    public string LogFile => Path.Combine(StateDirectory, "updater.log");

    /// <summary>
    /// Creates the state directory and makes it ignore itself. The installation is a git
    /// checkout and an update refuses to run on a dirty one - without this, the updater's own
    /// log and pid file would block every update it is supposed to perform. A .gitignore of
    /// "*" inside the folder covers the folder's contents and the file itself, so this works
    /// on installations whose checked-out .gitignore knows nothing about it.
    /// </summary>
    public void EnsureStateDirectory()
    {
        Directory.CreateDirectory(StateDirectory);

        var ignore = Path.Combine(StateDirectory, ".gitignore");
        if (!File.Exists(ignore)) File.WriteAllText(ignore, "*" + Environment.NewLine);
    }

    /// <summary>
    /// Finds the installation: an explicit path, then SNACKBOX_HOME, then the first directory
    /// at or above the updater itself that looks like the repository.
    /// </summary>
    public static Installation Resolve(string? explicitPath)
    {
        var candidate = explicitPath ?? Environment.GetEnvironmentVariable("SNACKBOX_HOME");
        if (!string.IsNullOrWhiteSpace(candidate))
        {
            var full = Path.GetFullPath(candidate);
            if (!IsInstallation(full)) throw new UpdaterException($"{full} is not a Snackbox installation (no Snackbox.sln next to a .git folder).");
            var named = new Installation(full);
            named.EnsureStateDirectory();
            return named;
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (IsInstallation(directory.FullName))
            {
                var found = new Installation(directory.FullName);
                found.EnsureStateDirectory();
                return found;
            }

            directory = directory.Parent;
        }

        throw new UpdaterException("Could not find the Snackbox installation. Pass --dir or set SNACKBOX_HOME.");
    }

    private static bool IsInstallation(string path)
    {
        var git = Path.Combine(path, ".git");

        // A clone has a .git directory, a git worktree has a .git file pointing at one - the
        // updater has to recognise both or it walks up into the surrounding repository.
        return (Directory.Exists(git) || File.Exists(git)) && File.Exists(Path.Combine(path, "Snackbox.sln"));
    }

    // ------------------------------------------------------------------ git

    /// <summary>The checked out release tag, or the short commit when running off a branch.</summary>
    public string CurrentVersion()
    {
        var tag = Git("describe --tags --exact-match HEAD", throwOnError: false).Trim();
        if (!string.IsNullOrEmpty(tag)) return tag;

        var described = Git("describe --tags --always", throwOnError: false).Trim();
        return string.IsNullOrEmpty(described) ? "unknown" : described;
    }

    public string CurrentCommit() => Git("rev-parse HEAD").Trim();

    public string Git(string arguments, bool throwOnError = true) =>
        Run("git", arguments, Root, throwOnError);

    // -------------------------------------------------------------- process

    public static string Run(string file, string arguments, string workingDirectory, bool throwOnError = true)
    {
        var info = new ProcessStartInfo(file, arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(info) ?? throw new UpdaterException($"Could not start {file}.");
        var output = new StringBuilder();
        output.Append(process.StandardOutput.ReadToEnd());
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            if (throwOnError)
            {
                throw new UpdaterException($"{file} {arguments} failed with exit code {process.ExitCode}.{Environment.NewLine}{error}{output}");
            }

            return "";
        }

        if (error.Length > 0) output.AppendLine(error);
        return output.ToString();
    }

    // ----------------------------------------------------- running processes

    public void RememberProcess(int pid)
    {
        EnsureStateDirectory();
        File.AppendAllLines(PidFile, [pid.ToString()]);
    }

    public IReadOnlyList<int> RememberedProcesses()
    {
        if (!File.Exists(PidFile)) return [];
        return File.ReadAllLines(PidFile)
                   .Select(line => int.TryParse(line.Trim(), out var pid) ? pid : 0)
                   .Where(pid => pid > 0)
                   .ToList();
    }

    public void ForgetProcesses()
    {
        if (File.Exists(PidFile)) File.Delete(PidFile);
    }
}

public sealed class UpdaterException(string message) : Exception(message);
