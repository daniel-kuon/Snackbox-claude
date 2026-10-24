using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Snackbox.Api.Dtos;

namespace Snackbox.Api.Services;

/// <summary>
/// Off-site copies of the backups: every automatic backup is encrypted with the admin's
/// passphrase (<see cref="BackupEncryption"/>) and uploaded to a connected OneDrive, which keeps
/// the newest <c>Backup:OneDrive:Keep</c> (30) files.
///
/// Signing in uses the OAuth device code flow: the page shows a code, the admin enters it at
/// microsoft.com/devicelogin on any device - nothing has to redirect back to the kiosk PC. It
/// needs an app registration of the admin's own (client id; see docs/ONEDRIVE-BACKUP.md), as
/// Microsoft has no shared client id for third-party apps. Plain HTTP against the documented
/// endpoints, no SDK.
///
/// The refresh token and the passphrase are stored with ASP.NET Data Protection (DPAPI on
/// Windows) in %ProgramData%\Snackbox\onedrive.json - useless on any other machine, which is
/// also why the passphrase has to be kept somewhere else as well.
///
/// Deliberately not [Traced]: the arguments include the passphrase.
/// </summary>
public class OneDriveBackup(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    IDataProtectionProvider dataProtection,
    ILogger<OneDriveBackup> logger)
{

    private const string Graph = "https://graph.microsoft.com/v1.0";
    // Files.ReadWrite rather than the app-folder scope: the app folder is not available on every
    // OneDrive for Business tenant. offline_access gives the refresh token.
    private const string Scopes = "Files.ReadWrite offline_access";
    private const string Folder = "Snackbox-Backups";
    public const int MinPassphraseLength = 12;

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static PendingSignIn? _pending;

    private readonly IDataProtector _protector = dataProtection.CreateProtector("Snackbox.OneDriveBackup");

    private record PendingSignIn(string ClientId, string DeviceCode, DateTime ExpiresAt);

    private string StatePath => configuration["Backup:OneDrive:StateFile"] ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Snackbox", "onedrive.json");

    private int Keep => configuration.GetValue("Backup:OneDrive:Keep", 30);

    // "common" serves registrations for "any organization and personal accounts"; a registration
    // for one organization only needs its tenant id here
    private string Authority => $"https://login.microsoftonline.com/{configuration["Backup:OneDrive:Tenant"] ?? "common"}/oauth2/v2.0";

    // --- state ---------------------------------------------------------------------------

    private class State
    {
        public string? ClientId { get; set; }
        public string? Account { get; set; }
        public string? ProtectedRefreshToken { get; set; }
        public string? ProtectedPassphrase { get; set; }
        public DateTime? LastUploadAt { get; set; }
        public string? LastUploadFile { get; set; }
        public string? LastError { get; set; }
        public DateTime? LastErrorAt { get; set; }
    }

    private State Load()
    {
        try
        {
            return File.Exists(StatePath) ? JsonSerializer.Deserialize<State>(File.ReadAllText(StatePath)) ?? new State() : new State();
        }
        catch (JsonException)
        {
            return new State();
        }
    }

    private void Save(State state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
        File.WriteAllText(StatePath, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
    }

    public OneDriveStatusDto GetStatus()
    {
        var state = Load();
        return new OneDriveStatusDto
        {
            ClientId = state.ClientId,
            Connected = state.ProtectedRefreshToken != null,
            Account = state.Account,
            HasPassphrase = state.ProtectedPassphrase != null,
            LastUploadAt = state.LastUploadAt,
            LastUploadFile = state.LastUploadFile,
            LastError = state.LastError,
            LastErrorAt = state.LastErrorAt,
            Keep = Keep
        };
    }

    public async Task SaveSettingsAsync(OneDriveSettingsDto settings)
    {
        await Gate.WaitAsync();
        try
        {
            var state = Load();
            var clientId = settings.ClientId?.Trim();
            if (!string.IsNullOrEmpty(clientId) && clientId != state.ClientId)
            {
                if (!Guid.TryParse(clientId, out _)) throw new ArgumentException("The client id is a GUID (Application (client) ID of the app registration).");
                // A token belongs to the app registration it was issued for
                state.ClientId = clientId;
                state.ProtectedRefreshToken = null;
                state.Account = null;
            }

            if (!string.IsNullOrEmpty(settings.Passphrase))
            {
                if (settings.Passphrase.Length < MinPassphraseLength)
                    throw new ArgumentException($"The passphrase needs at least {MinPassphraseLength} characters.");
                state.ProtectedPassphrase = _protector.Protect(settings.Passphrase);
            }

            Save(state);
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <summary>The passphrase this machine encrypts with - also tried when an encrypted backup is imported.</summary>
    public string? Passphrase
    {
        get
        {
            var state = Load();
            try
            {
                return state.ProtectedPassphrase == null ? null : _protector.Unprotect(state.ProtectedPassphrase);
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                return null;
            }
        }
    }

    // --- sign-in (device code flow) -------------------------------------------------------

    public async Task<OneDriveSignInDto> StartSignInAsync()
    {
        var clientId = Load().ClientId ?? throw new InvalidOperationException("Enter the client id of your app registration first.");

        using var http = httpClientFactory.CreateClient();
        using var response = await http.PostAsync($"{Authority}/devicecode",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["client_id"] = clientId, ["scope"] = Scopes }));
        var body = await response.Content.ReadFromJsonAsync<DeviceCodeResponse>();
        if (!response.IsSuccessStatusCode || body?.DeviceCode == null)
            throw new InvalidOperationException($"Microsoft refused the sign-in: {body?.ErrorDescription ?? response.StatusCode.ToString()}");

        _pending = new PendingSignIn(clientId, body.DeviceCode, DateTime.UtcNow.AddSeconds(body.ExpiresIn));
        return new OneDriveSignInDto
        {
            UserCode = body.UserCode!,
            VerificationUri = body.VerificationUri ?? "https://microsoft.com/devicelogin",
            ExpiresInSeconds = body.ExpiresIn,
            IntervalSeconds = Math.Max(body.Interval, 5)
        };
    }

    /// <summary>One poll: "pending" until the admin finished signing in, then "connected".</summary>
    public async Task<string> PollSignInAsync()
    {
        var pending = _pending;
        if (pending == null || pending.ExpiresAt < DateTime.UtcNow) return "expired";

        using var http = httpClientFactory.CreateClient();
        using var response = await http.PostAsync($"{Authority}/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
            ["client_id"] = pending.ClientId,
            ["device_code"] = pending.DeviceCode
        }));
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();

        if (token?.Error is "authorization_pending" or "slow_down") return "pending";
        if (token?.RefreshToken == null || token.AccessToken == null)
        {
            _pending = null;
            return $"failed: {token?.ErrorDescription ?? token?.Error ?? response.StatusCode.ToString()}";
        }

        _pending = null;
        var account = await GetAccountAsync(token.AccessToken);

        await Gate.WaitAsync();
        try
        {
            var state = Load();
            state.ProtectedRefreshToken = _protector.Protect(token.RefreshToken);
            state.Account = account;
            state.LastError = null;
            Save(state);
        }
        finally
        {
            Gate.Release();
        }

        logger.LogInformation("OneDrive backup connected to {Account}", account);
        return "connected";
    }

    public async Task DisconnectAsync()
    {
        await Gate.WaitAsync();
        try
        {
            var state = Load();
            state.ProtectedRefreshToken = null;
            state.Account = null;
            Save(state);
        }
        finally
        {
            Gate.Release();
        }
    }

    // --- upload ---------------------------------------------------------------------------

    /// <summary>
    /// Encrypts and uploads a backup file, then deletes the oldest uploads beyond the limit.
    /// Records the outcome for the Backups page; never throws - a failed upload must not fail
    /// the local backup.
    /// </summary>
    public async Task<bool> UploadAsync(string filePath)
    {
        var state = Load();
        if (state.ProtectedRefreshToken == null || state.ClientId == null) return false;

        await Gate.WaitAsync();
        try
        {
            var passphrase = Passphrase ?? throw new InvalidOperationException("No passphrase set - backups are only uploaded encrypted.");
            var accessToken = await RefreshAsync(state);

            var encrypted = BackupEncryption.Encrypt(await File.ReadAllBytesAsync(filePath), passphrase);
            var name = Path.GetFileName(filePath) + BackupEncryption.FileExtension;

            using var http = httpClientFactory.CreateClient();
            http.Timeout = TimeSpan.FromMinutes(10);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Simple upload: fine up to 250 MB, the database dumps are a few MB
            using (var content = new ByteArrayContent(encrypted))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                using var put = await http.PutAsync($"{Graph}/me/drive/root:/{Folder}/{Uri.EscapeDataString(name)}:/content", content);
                if (!put.IsSuccessStatusCode)
                    throw new InvalidOperationException($"OneDrive refused the upload: {(int)put.StatusCode} {await put.Content.ReadAsStringAsync()}");
            }

            await DeleteOldUploadsAsync(http);

            state = Load();
            state.LastUploadAt = DateTime.UtcNow;
            state.LastUploadFile = name;
            state.LastError = null;
            Save(state);
            logger.LogInformation("Backup {File} uploaded to OneDrive ({Size} bytes encrypted)", name, encrypted.Length);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uploading the backup {File} to OneDrive failed", filePath);
            state = Load();
            state.LastError = ex.Message;
            state.LastErrorAt = DateTime.UtcNow;
            Save(state);
            return false;
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <summary>A fresh access token; the refresh token is rotated and stored again.</summary>
    private async Task<string> RefreshAsync(State state)
    {
        using var http = httpClientFactory.CreateClient();
        using var response = await http.PostAsync($"{Authority}/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = state.ClientId!,
            ["refresh_token"] = _protector.Unprotect(state.ProtectedRefreshToken!),
            ["scope"] = Scopes
        }));
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        if (token?.AccessToken == null)
            throw new InvalidOperationException($"The OneDrive sign-in is no longer valid - connect again ({token?.ErrorDescription ?? token?.Error ?? response.StatusCode.ToString()})");

        if (token.RefreshToken != null)
        {
            var latest = Load();
            latest.ProtectedRefreshToken = _protector.Protect(token.RefreshToken);
            Save(latest);
        }
        return token.AccessToken;
    }

    private async Task DeleteOldUploadsAsync(HttpClient http)
    {
        var files = new List<DriveItem>();
        string? next = $"{Graph}/me/drive/root:/{Folder}:/children?$select=id,name&$top=200";
        while (next != null)
        {
            using var response = await http.GetAsync(next);
            if (!response.IsSuccessStatusCode) return; // retention is best effort
            var page = await response.Content.ReadFromJsonAsync<DriveItemPage>();
            files.AddRange(page?.Value ?? []);
            next = page?.NextLink;
        }

        // Names start with snackbox_backup_yyyyMMdd_HHmmss, so they sort by age
        var ours = files.Where(f => f.Name?.StartsWith("snackbox_backup_") == true && f.Name.EndsWith(BackupEncryption.FileExtension))
                        .OrderByDescending(f => f.Name, StringComparer.Ordinal)
                        .ToList();
        foreach (var old in ours.Skip(Keep))
        {
            using var _ = await http.DeleteAsync($"{Graph}/me/drive/items/{old.Id}");
            logger.LogInformation("Deleted old OneDrive backup {File}", old.Name);
        }
    }

    private async Task<string> GetAccountAsync(string accessToken)
    {
        using var http = httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await http.GetAsync($"{Graph}/me/drive?$select=owner,driveType");
        var drive = response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<Drive>() : null;
        var user = drive?.Owner?.User;
        return user?.Email ?? user?.DisplayName ?? "OneDrive";
    }

    // --- wire formats -----------------------------------------------------------------------

    private class DeviceCodeResponse
    {
        [JsonPropertyName("device_code")] public string? DeviceCode { get; set; }
        [JsonPropertyName("user_code")] public string? UserCode { get; set; }
        [JsonPropertyName("verification_uri")] public string? VerificationUri { get; set; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("interval")] public int Interval { get; set; }
        [JsonPropertyName("error_description")] public string? ErrorDescription { get; set; }
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
        [JsonPropertyName("error")] public string? Error { get; set; }
        [JsonPropertyName("error_description")] public string? ErrorDescription { get; set; }
    }

    private class DriveItem
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    private class DriveItemPage
    {
        [JsonPropertyName("value")] public List<DriveItem>? Value { get; set; }
        [JsonPropertyName("@odata.nextLink")] public string? NextLink { get; set; }
    }

    private class Drive
    {
        [JsonPropertyName("owner")] public IdentitySet? Owner { get; set; }
    }

    private class IdentitySet
    {
        [JsonPropertyName("user")] public Identity? User { get; set; }
    }

    private class Identity
    {
        [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
    }
}
