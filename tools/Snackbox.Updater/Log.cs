namespace Snackbox.Updater;

/// <summary>
/// Writes to the console and to a file inside the installation. The admin page reads the file
/// back, because the update runs after the app that triggered it has been stopped - the log is
/// the only place the result can be seen afterwards.
/// </summary>
public sealed class Log(string path)
{
    private readonly Lock _lock = new();

    public string Path { get; } = path;

    public void Write(string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";
        Console.WriteLine(line);
        Append(line);
    }

    /// <summary>Output of a child process - already formatted, and dropped when empty.</summary>
    public void WriteRaw(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        Append(message);
    }

    private void Append(string line)
    {
        try
        {
            lock (_lock)
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
                File.AppendAllLines(Path, [line]);
            }
        }
        catch
        {
            // Logging must never be the reason an update fails.
        }
    }
}
