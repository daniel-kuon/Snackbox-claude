using Snackbox.Updater;

// Snackbox updater. Runs outside the app so it can stop it, rebuild the checkout and start it
// again. Commands:
//
//   status                         what is installed and what is running
//   update --tag v1.2.3            stop, check the tag out, build, start again
//   start [--no-kiosk]             Docker, then the Aspire AppHost, then the kiosk
//   stop                           stop everything this tool started
//   install-autostart [--no-kiosk] register a scheduled task that runs "start" at logon
//   uninstall-autostart            remove that task
//
// Every command takes --dir <installation>; it otherwise falls back to SNACKBOX_HOME and then
// to the repository the updater itself sits in.

var arguments = new Arguments(args);
var command = arguments.Command;

if (command is null or "help" or "--help" or "-h")
{
    PrintUsage();
    return 0;
}

Installation installation;
try
{
    installation = Installation.Resolve(arguments.Value("dir"));
}
catch (UpdaterException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 2;
}

var log = new Log(installation.LogFile);
var stack = new Stack(installation, log);
var withKiosk = !arguments.Flag("no-kiosk");

try
{
    switch (command)
    {
        case "status":
            Console.WriteLine($"Installation : {installation.Root}");
            Console.WriteLine($"Version      : {installation.CurrentVersion()}");
            Console.WriteLine($"Commit       : {installation.CurrentCommit()}");
            Console.WriteLine($"Running pids : {string.Join(", ", installation.RememberedProcesses())}");
            Console.WriteLine($"Log          : {log.Path}");
            return 0;

        case "start":
            stack.Start(withKiosk);
            return 0;

        case "stop":
            stack.Stop();
            return 0;

        case "update":
            var tag = arguments.Value("tag") ?? throw new UpdaterException("update needs --tag, for example --tag v1.2.3.");

            // The update rebuilds the whole solution, which includes this very executable, and
            // Windows will not overwrite a running file. Continue from a copy in the temp
            // folder. SelfCopy sets the marker below on the copy; it is not for callers, and
            // passing it by hand is how you get "the file is used by another process".
            if (!arguments.Flag(SelfCopy.Marker) && SelfCopy.Relaunch(args, log)) return 0;

            var result = new UpdateCommand(installation, stack, log).Run(tag, withKiosk, start: !arguments.Flag("no-start"));
            if (arguments.Flag("keep-window")) KeepWindowOpen(result);
            return result;

        case "install-autostart":
            Autostart.Install(installation, withKiosk, log);
            return 0;

        case "uninstall-autostart":
            Autostart.Uninstall(log);
            return 0;

        default:
            Console.Error.WriteLine($"Unknown command: {command}");
            PrintUsage();
            return 2;
    }
}
catch (UpdaterException ex)
{
    log.Write($"ERROR: {ex.Message}");
    return 1;
}
catch (Exception ex)
{
    log.Write($"ERROR: {ex}");
    return 1;
}

// Started from the Updates page, the update runs in its own terminal window. Without this the
// window would vanish the moment the update finishes - including when it failed.
static void KeepWindowOpen(int result)
{
    Console.WriteLine();
    if (result == 0)
    {
        Console.WriteLine("Done. Snackbox is starting - this window closes in 60 seconds.");
        Thread.Sleep(TimeSpan.FromSeconds(60));
        return;
    }

    Console.WriteLine("The update did NOT complete - see the messages above.");
    Console.WriteLine("Press Enter to close this window.");
    try
    {
        Console.ReadLine();
    }
    catch
    {
        Thread.Sleep(TimeSpan.FromMinutes(30));
    }
}

static void PrintUsage()
{
    Console.WriteLine("""
        Snackbox updater

          status                          show the installed version and what is running
          update --tag <tag> [--no-start] [--keep-window]
                                          update the installation to a release tag
          start [--no-kiosk]              start Docker, the Aspire stack and the kiosk
          stop                            stop everything the updater started
          install-autostart [--no-kiosk]  run "start" at every logon
          uninstall-autostart             remove the autostart task

        Options
          --dir <path>                    the installation to act on (default: SNACKBOX_HOME,
                                          or the repository the updater sits in)
        """);
}

/// <summary>Minimal "command --key value --flag" parsing; no dependency is worth more here.</summary>
internal sealed class Arguments
{
    private readonly Dictionary<string, string?> _values = new(StringComparer.OrdinalIgnoreCase);

    public Arguments(string[] args)
    {
        if (args.Length > 0 && !args[0].StartsWith('-')) Command = args[0];

        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal)) continue;

            var key = args[i][2..];
            var hasValue = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal);
            _values[key] = hasValue ? args[++i] : null;
        }
    }

    public string? Command { get; }

    public string? Value(string key) => _values.GetValueOrDefault(key);

    public bool Flag(string key) => _values.ContainsKey(key);
}
