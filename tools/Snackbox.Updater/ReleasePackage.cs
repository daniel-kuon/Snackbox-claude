using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;

namespace Snackbox.Updater;

/// <summary>
/// The prebuilt binaries attached to every GitHub release (tools/package-release.ps1). With them
/// an update copies files instead of building the whole solution on the kiosk PC, which cost
/// several minutes of downtime. The AppHost alone is still built here: its build bakes in
/// absolute paths of the machine - of the projects, and of DCP and the dashboard in the NuGet
/// cache - and it is one small project.
/// </summary>
public sealed partial class ReleasePackage(Installation installation, Log log)
{
    /// <summary>The release asset's name - the release workflow uploads it under the same name.</summary>
    public static string AssetName(string tag) => $"snackbox-{tag}-win-x64.zip";

    /// <summary>
    /// Downloads and unpacks the release's package while the old version still runs - unless the
    /// API already did, in the background (UpdateService.DownloadLatestAsync, same folder). Null
    /// when there is none - an older release, a private repository, no network - and the update
    /// then builds from source as it always did.
    /// </summary>
    public string? Download(string tag)
    {
        var folder = Path.Combine(installation.StateDirectory, $"release-{tag}");
        if (Directory.Exists(folder))
        {
            log.Write("The release was already downloaded in the background.");
            return folder;
        }

        var repository = GitHubRepository(installation.Git("remote get-url origin", throwOnError: false).Trim());
        if (repository == null)
        {
            log.Write("origin is not a GitHub repository - building from source.");
            return null;
        }

        var url = $"https://github.com/{repository}/releases/download/{tag}/{AssetName(tag)}";
        var zip = Path.Combine(installation.StateDirectory, AssetName(tag));

        try
        {
            log.Write($"Downloading {url}...");
            using (var http = new HttpClient { Timeout = TimeSpan.FromMinutes(15) })
            using (var response = http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult())
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    log.Write("The release has no prebuilt package - building from source.");
                    return null;
                }

                response.EnsureSuccessStatusCode();
                using var file = File.Create(zip);
                response.Content.CopyToAsync(file).GetAwaiter().GetResult();
            }

            // Complete or not there at all: a folder of that name is taken as ready to install
            var partial = $"{folder}.partial-{Guid.NewGuid():N}";
            ZipFile.ExtractToDirectory(zip, partial);
            if (Directory.Exists(folder)) Directory.Delete(partial, recursive: true); // the API was quicker
            else Directory.Move(partial, folder);
            log.Write("Downloaded and unpacked.");
            return folder;
        }
        catch (Exception ex)
        {
            log.Write($"Could not get the prebuilt package ({ex.Message}) - building from source.");
            return null;
        }
        finally
        {
            File.Delete(zip);
        }
    }

    /// <summary>Copies the unpacked package over the checkout (stack stopped) and builds the AppHost.</summary>
    public void Install(string folder)
    {
        log.Write("Installing the prebuilt release...");
        var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);

        // A build's static web assets manifest, left over in bin, would win over the package's
        // published wwwroot - in the website and the kiosk - and point at what the package did
        // not build. Copied over rather than replaced, bin keeps the kiosk's WebView2 profile.
        foreach (var directory in files.Select(f => Path.GetDirectoryName(Path.GetRelativePath(folder, f))!).Distinct())
        {
            var target = Path.Combine(installation.Root, directory);
            if (!Directory.Exists(target)) continue;
            foreach (var manifest in Directory.GetFiles(target, "*.staticwebassets.runtime.json")) File.Delete(manifest);
        }

        foreach (var file in files)
        {
            var target = Path.Combine(installation.Root, Path.GetRelativePath(folder, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }

        log.Write($"Copied {files.Length} files. Building the AppHost...");

        // Not its project references: those are what the package just brought
        log.WriteRaw(Installation.Run("dotnet", "build src/Snackbox.AppHost/Snackbox.AppHost.csproj -c Release -p:BuildProjectReferences=false",
                                      installation.Root));
        Directory.Delete(folder, recursive: true);
        log.Write("Prebuilt release installed.");
    }

    /// <summary>"owner/repo" of an https or ssh GitHub remote, otherwise null.</summary>
    public static string? GitHubRepository(string remote)
    {
        var match = GitHubRemote().Match(remote);
        return match.Success ? match.Groups["repo"].Value : null;
    }

    [GeneratedRegex(@"github\.com[/:](?<repo>[\w.-]+/[\w.-]+?)(\.git)?/?$", RegexOptions.IgnoreCase)]
    private static partial Regex GitHubRemote();
}
