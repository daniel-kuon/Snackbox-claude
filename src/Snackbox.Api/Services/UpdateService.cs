using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;

namespace Snackbox.Api.Services;

public class UpdateService : IUpdateService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IBackupService _backupService;
    private readonly UpdateSettings _settings;
    private readonly ILogger<UpdateService> _logger;

    public UpdateService(
        IHttpClientFactory httpClientFactory,
        IBackupService backupService,
        IOptions<UpdateSettings> settings,
        ILogger<UpdateService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _backupService = backupService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<UpdateCheckResultDto> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var release = await GetReleaseAsync(version: null, cancellationToken);
        var latestVersion = NormalizeVersion(release.TagName);
        var currentVersion = GetCurrentVersion();

        return new UpdateCheckResultDto
        {
            CurrentVersion = currentVersion,
            LatestVersion = latestVersion,
            UpdateAvailable = CompareVersions(currentVersion, latestVersion) < 0,
            LatestReleaseNotes = release.Body,
            LatestPublishedAt = release.PublishedAt
        };
    }

    public async Task<UpdateApplyResultDto> ApplyUpdateAsync(UpdateApplyRequestDto request, CancellationToken cancellationToken = default)
    {
        var release = await GetReleaseAsync(request.Version, cancellationToken);
        var targetVersion = NormalizeVersion(release.TagName);
        var currentVersion = GetCurrentVersion();

        if (CompareVersions(currentVersion, targetVersion) > 0 && !request.AllowDowngrade)
        {
            throw new InvalidOperationException($"Target version {targetVersion} is older than current version {currentVersion}. Enable AllowDowngrade to proceed.");
        }

        if (!await _backupService.ArePostgresToolsAvailableAsync())
        {
            throw new InvalidOperationException("PostgreSQL tools are not available. Install PostgreSQL 17 or run scripts/Install-PostgresTools.ps1 before updating.");
        }

        var backup = await _backupService.CreateBackupAsync(BackupType.Manual, $"pre-update-{targetVersion}");

        var installPath = ResolveInstallPath();
        var scriptPath = await ResolveInstallScriptPathAsync(cancellationToken);

        StartInstallProcess(scriptPath, installPath, targetVersion, request.AllowDowngrade);

        return new UpdateApplyResultDto
        {
            Message = "Update started. The installer will download and apply the selected version.",
            TargetVersion = targetVersion,
            BackupCreated = backup != null,
            BackupId = backup?.Id
        };
    }

    private async Task<GitHubRelease> GetReleaseAsync(string? version, CancellationToken cancellationToken)
    {
        var requestedTag = string.IsNullOrWhiteSpace(version) ? null : NormalizeTag(version);
        var apiUrl = requestedTag == null
            ? $"https://api.github.com/repos/{_settings.RepoOwner}/{_settings.RepoName}/releases/latest"
            : $"https://api.github.com/repos/{_settings.RepoOwner}/{_settings.RepoName}/releases/tags/{requestedTag}";

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Snackbox-Update-Service");

        using var response = await client.GetAsync(apiUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, cancellationToken: cancellationToken);
        if (release == null)
        {
            throw new InvalidOperationException("Failed to parse release information from GitHub.");
        }

        return release;
    }

    private string ResolveInstallPath()
    {
        if (!string.IsNullOrWhiteSpace(_settings.InstallPath))
        {
            return _settings.InstallPath;
        }

        var baseDir = AppContext.BaseDirectory;
        var parent = Directory.GetParent(baseDir)?.FullName;
        return parent ?? baseDir;
    }

    private async Task<string> ResolveInstallScriptPathAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_settings.InstallScriptPath) && File.Exists(_settings.InstallScriptPath))
        {
            return _settings.InstallScriptPath;
        }

        var installPath = ResolveInstallPath();
        var localScriptPath = Path.Combine(installPath, "install-snackbox.ps1");
        if (File.Exists(localScriptPath))
        {
            return localScriptPath;
        }

        var tempScriptPath = Path.Combine(Path.GetTempPath(), $"install-snackbox-{Guid.NewGuid():N}.ps1");
        var scriptUrl = $"https://raw.githubusercontent.com/{_settings.RepoOwner}/{_settings.RepoName}/main/install-snackbox.ps1";

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Snackbox-Update-Service");
        var scriptContents = await client.GetStringAsync(scriptUrl, cancellationToken);
        await File.WriteAllTextAsync(tempScriptPath, scriptContents, cancellationToken);

        return tempScriptPath;
    }

    private void StartInstallProcess(string scriptPath, string installPath, string targetVersion, bool allowDowngrade)
    {
        var args = new List<string>
        {
            "-NoProfile",
            "-ExecutionPolicy", "Bypass",
            "-File", $"\"{scriptPath}\"",
            "-InstallPath", $"\"{installPath}\"",
            "-RepoOwner", _settings.RepoOwner,
            "-RepoName", _settings.RepoName,
            "-Version", targetVersion,
            "-StopRunning",
            "-RestartAppHost"
        };

        if (allowDowngrade)
        {
            args.Add("-AllowDowngrade");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = string.Join(" ", args),
            CreateNoWindow = true,
            UseShellExecute = false
        };

        var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start the installer process.");
        }

        _logger.LogInformation("Started update installer for version {Version} at {InstallPath}", targetVersion, installPath);
    }

    private static string? GetCurrentVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(infoVersion))
        {
            var plusIndex = infoVersion.IndexOf('+');
            return plusIndex > 0 ? infoVersion[..plusIndex] : infoVersion;
        }

        return assembly.GetName().Version?.ToString();
    }

    private static string NormalizeTag(string version)
    {
        var trimmed = version.Trim();
        return trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? trimmed : $"v{trimmed}";
    }

    private static string? NormalizeVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return version;
        }

        var trimmed = version.Trim().TrimStart('v', 'V');
        var plusIndex = trimmed.IndexOf('+');
        return plusIndex > 0 ? trimmed[..plusIndex] : trimmed;
    }

    private static int CompareVersions(string? current, string? latest)
    {
        if (!Version.TryParse(current, out var currentVersion))
        {
            return string.IsNullOrWhiteSpace(current) ? -1 : 0;
        }

        if (!Version.TryParse(latest, out var latestVersion))
        {
            return 0;
        }

        return currentVersion.CompareTo(latestVersion);
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("published_at")]
        public DateTimeOffset? PublishedAt { get; set; }
    }
}
