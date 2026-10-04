using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using Snackbox.Api.Dtos;
using Snackbox.ServiceDefaults.Tracing;

namespace Snackbox.Api.Services;

public interface IUpdateService
{
    Task<UpdateStatusDto> GetStatusAsync(CancellationToken ct = default);
    Task<InstallUpdateResponseDto> InstallAsync(string? tag, CancellationToken ct = default);
    UpdateLogDto GetLog(int lines = 200);
}

/// <summary>
/// An installation is a git checkout of the repository that is built and run in place, so the
/// installed version is simply the release tag that is checked out. Updating is handed to
/// tools/Snackbox.Updater, which has to run outside this process - it stops the app it updates.
/// </summary>
[Traced]
public class UpdateService : IUpdateService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UpdateService> _logger;

    public UpdateService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<UpdateService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    private string Repository => _configuration.GetValue("Update:Repository", "daniel-kuon/Snackbox-claude")!;

    private bool IncludePreReleases => _configuration.GetValue("Update:IncludePreReleases", false);

    // ---------------------------------------------------------------- status

    public async Task<UpdateStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        var root = FindInstallationRoot();
        var status = new UpdateStatusDto
        {
            CurrentVersion = root == null ? "development build" : CurrentVersion(root),
            CanUpdate = root != null,
            Blocker = root == null ? "This is not a git checkout, so there is nothing to update in place." : null
        };

        if (root != null)
        {
            var dirty = Git(root, "status --porcelain").Trim();
            if (dirty.Length > 0)
            {
                status.CanUpdate = false;
                status.Blocker = "The installation has uncommitted local changes.";
            }
        }

        try
        {
            status.Latest = await GetLatestReleaseAsync(ct);
            status.UpdateAvailable = status.Latest != null && IsNewer(status.Latest.Tag, status.CurrentVersion);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not check {Repository} for releases", Repository);
            status.CheckError = ex.Message;
        }

        return status;
    }

    private async Task<ReleaseDto?> GetLatestReleaseAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(20);
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Snackbox", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        // A token is only needed for a private repository or to raise the rate limit.
        var token = _configuration["Update:GitHubToken"];
        if (!string.IsNullOrWhiteSpace(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await client.GetAsync($"https://api.github.com/repos/{Repository}/releases?per_page=20", ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        foreach (var release in document.RootElement.EnumerateArray())
        {
            if (release.GetProperty("draft").GetBoolean()) continue;

            var preRelease = release.GetProperty("prerelease").GetBoolean();
            if (preRelease && !IncludePreReleases) continue;

            return new ReleaseDto
            {
                Tag = release.GetProperty("tag_name").GetString() ?? "",
                Name = release.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                Notes = release.TryGetProperty("body", out var body) ? body.GetString() : null,
                PublishedAt = release.TryGetProperty("published_at", out var published) && published.ValueKind == JsonValueKind.String
                    ? published.GetDateTime()
                    : null,
                Url = release.TryGetProperty("html_url", out var url) ? url.GetString() : null,
                PreRelease = preRelease
            };
        }

        return null;
    }

    /// <summary>
    /// Compares release tags. An installation sitting on a bare commit has no version to
    /// compare against, so any release counts as newer - that is the "I am off a branch" case.
    /// </summary>
    public static bool IsNewer(string candidateTag, string currentTag)
    {
        if (string.Equals(candidateTag, currentTag, StringComparison.OrdinalIgnoreCase)) return false;
        if (!TryParseTag(candidateTag, out var candidate)) return false;
        if (!TryParseTag(currentTag, out var current)) return true;

        return candidate > current;
    }

    private static bool TryParseTag(string tag, out Version version)
    {
        version = new Version(0, 0);
        if (string.IsNullOrWhiteSpace(tag)) return false;

        var trimmed = tag.Trim();
        if (trimmed.StartsWith('v') || trimmed.StartsWith('V')) trimmed = trimmed[1..];

        // Drop a pre-release suffix such as -rc1; Version cannot parse it.
        var dash = trimmed.IndexOf('-');
        if (dash > 0) trimmed = trimmed[..dash];

        return Version.TryParse(trimmed, out version!);
    }

    // --------------------------------------------------------------- install

    public async Task<InstallUpdateResponseDto> InstallAsync(string? tag, CancellationToken ct = default)
    {
        var root = FindInstallationRoot()
                   ?? throw new InvalidOperationException("This is not a git checkout, so it cannot update itself.");

        if (string.IsNullOrWhiteSpace(tag))
        {
            var latest = await GetLatestReleaseAsync(ct);
            tag = latest?.Tag ?? throw new InvalidOperationException($"{Repository} has no published release to install.");
        }

        var updater = FindUpdater(root)
                      ?? throw new InvalidOperationException(
                          "Snackbox.Updater was not found. Build the solution so tools/Snackbox.Updater exists.");

        // Detached on purpose: the first thing the updater does is stop this process, so the
        // response has to be on its way before it runs.
        var info = new ProcessStartInfo(updater)
        {
            WorkingDirectory = root,
            UseShellExecute = true,
            CreateNoWindow = true
        };
        info.ArgumentList.Add("update");
        info.ArgumentList.Add("--tag");
        info.ArgumentList.Add(tag);
        info.ArgumentList.Add("--dir");
        info.ArgumentList.Add(root);

        _logger.LogWarning("Starting update to {Tag} via {Updater}; the stack is about to be restarted", tag, updater);
        Process.Start(info);

        return new InstallUpdateResponseDto
        {
            Started = true,
            Tag = tag,
            Message = "The updater is running. Snackbox stops, rebuilds and starts again - this takes a few minutes."
        };
    }

    public UpdateLogDto GetLog(int lines = 200)
    {
        var root = FindInstallationRoot();
        if (root == null) return new UpdateLogDto();

        var path = Path.Combine(root, ".snackbox", "updater.log");
        if (!File.Exists(path)) return new UpdateLogDto();

        try
        {
            // The updater may be writing while we read, hence the sharing flags.
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            var all = reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);

            return new UpdateLogDto
            {
                Lines = all.TakeLast(lines).Select(l => l.TrimEnd('\r')).ToArray(),
                LastWrite = File.GetLastWriteTime(path)
            };
        }
        catch (IOException)
        {
            return new UpdateLogDto();
        }
    }

    // ----------------------------------------------------------------- paths

    /// <summary>
    /// Walks up from the running assembly looking for the repository. A git worktree has a
    /// .git file rather than a directory, so both are accepted.
    /// </summary>
    private string? FindInstallationRoot()
    {
        var configured = _configuration["Update:InstallRoot"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return IsInstallation(configured) ? Path.GetFullPath(configured) : null;
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (IsInstallation(directory.FullName)) return directory.FullName;
            directory = directory.Parent;
        }

        return null;
    }

    private static bool IsInstallation(string path)
    {
        var git = Path.Combine(path, ".git");
        return (Directory.Exists(git) || File.Exists(git)) && File.Exists(Path.Combine(path, "Snackbox.sln"));
    }

    private static string? FindUpdater(string root)
    {
        string[] candidates =
        [
            Path.Combine(root, "tools", "Snackbox.Updater", "bin", "Release", "net10.0", "Snackbox.Updater.exe"),
            Path.Combine(root, "tools", "Snackbox.Updater", "bin", "Debug", "net10.0", "Snackbox.Updater.exe")
        ];

        return candidates.FirstOrDefault(File.Exists);
    }

    private static string CurrentVersion(string root)
    {
        var tag = Git(root, "describe --tags --exact-match HEAD").Trim();
        if (!string.IsNullOrEmpty(tag)) return tag;

        var described = Git(root, "describe --tags --always").Trim();
        return string.IsNullOrEmpty(described) ? "unknown" : described;
    }

    private static string Git(string root, string arguments)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process == null) return "";

            var output = process.StandardOutput.ReadToEnd();
            process.StandardError.ReadToEnd();
            process.WaitForExit(10_000);
            return process.ExitCode == 0 ? output : "";
        }
        catch
        {
            return "";
        }
    }
}
