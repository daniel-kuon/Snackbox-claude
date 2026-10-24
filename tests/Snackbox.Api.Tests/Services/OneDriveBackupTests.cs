using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Snackbox.Api.Dtos;
using Snackbox.Api.Services;
using Xunit;

namespace Snackbox.Api.Tests.Services;

public class BackupEncryptionTests
{
    private static readonly byte[] Sql = Encoding.UTF8.GetBytes("-- PostgreSQL database dump\nCOPY users ...");

    [Fact]
    public void RoundTrip_GivesTheSameBytesBack()
    {
        var encrypted = BackupEncryption.Encrypt(Sql, "correct horse battery");

        Assert.True(BackupEncryption.IsEncrypted(encrypted));
        Assert.False(BackupEncryption.IsEncrypted(Sql));
        Assert.DoesNotContain("PostgreSQL", Encoding.UTF8.GetString(encrypted));
        Assert.Equal(Sql, BackupEncryption.Decrypt(encrypted, "correct horse battery"));
    }

    [Fact]
    public void WrongPassphrase_OrChangedFile_IsRefused()
    {
        var encrypted = BackupEncryption.Encrypt(Sql, "correct horse battery");
        Assert.Throws<CryptographicException>(() => BackupEncryption.Decrypt(encrypted, "wrong horse battery"));

        encrypted[^1] ^= 1;
        Assert.Throws<CryptographicException>(() => BackupEncryption.Decrypt(encrypted, "correct horse battery"));
    }
}

public class OneDriveBackupTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "snackbox-onedrive-test-" + Guid.NewGuid().ToString("N"));
    private readonly FakeMicrosoft _microsoft = new();
    private readonly OneDriveBackup _oneDrive;

    public OneDriveBackupTests()
    {
        Directory.CreateDirectory(_dir);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Backup:OneDrive:StateFile"] = Path.Combine(_dir, "onedrive.json"),
            ["Backup:OneDrive:Keep"] = "2"
        }).Build();
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(_microsoft));
        _oneDrive = new OneDriveBackup(configuration, factory.Object, new EphemeralDataProtectionProvider(), NullLogger<OneDriveBackup>.Instance);
    }

    [Fact]
    public async Task Settings_RefuseAShortPassphraseAndABadClientId()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _oneDrive.SaveSettingsAsync(new OneDriveSettingsDto { Passphrase = "short" }));
        await Assert.ThrowsAsync<ArgumentException>(() => _oneDrive.SaveSettingsAsync(new OneDriveSettingsDto { ClientId = "not-a-guid" }));
    }

    [Fact]
    public async Task SignIn_ThenUpload_SendsTheBackupEncryptedAndKeepsTheNewestOnly()
    {
        await _oneDrive.SaveSettingsAsync(new OneDriveSettingsDto { ClientId = Guid.NewGuid().ToString(), Passphrase = "correct horse battery" });

        var signIn = await _oneDrive.StartSignInAsync();
        Assert.Equal("ABCD-EFGH", signIn.UserCode);
        _microsoft.SignedIn = false;
        Assert.Equal("pending", await _oneDrive.PollSignInAsync());
        _microsoft.SignedIn = true;
        Assert.Equal("connected", await _oneDrive.PollSignInAsync());
        Assert.Equal("jane@example.com", _oneDrive.GetStatus().Account);

        // Stored, but not readable as such
        var stateFile = await File.ReadAllTextAsync(Path.Combine(_dir, "onedrive.json"));
        Assert.DoesNotContain("refresh-1", stateFile);
        Assert.DoesNotContain("correct horse battery", stateFile);

        var backup = Path.Combine(_dir, "snackbox_backup_20261024_030000_Daily.sql");
        await File.WriteAllTextAsync(backup, "-- PostgreSQL database dump");

        Assert.True(await _oneDrive.UploadAsync(backup));

        Assert.EndsWith("/Snackbox-Backups/snackbox_backup_20261024_030000_Daily.sql.enc:/content", _microsoft.UploadedTo);
        Assert.Equal("-- PostgreSQL database dump", Encoding.UTF8.GetString(BackupEncryption.Decrypt(_microsoft.Uploaded!, "correct horse battery")));
        // Three uploads there (two old ones and this one), two kept: the oldest goes
        Assert.Equal(new[] { "old-1" }, _microsoft.Deleted);
        // The rotated refresh token is the one used from now on
        Assert.Equal("refresh-1", _microsoft.RefreshTokensUsed.Single());
        Assert.Null(_oneDrive.GetStatus().LastError);
    }

    [Fact]
    public async Task FailedUpload_IsRecordedNotThrown()
    {
        await _oneDrive.SaveSettingsAsync(new OneDriveSettingsDto { ClientId = Guid.NewGuid().ToString(), Passphrase = "correct horse battery" });
        await _oneDrive.StartSignInAsync();
        await _oneDrive.PollSignInAsync();
        _microsoft.UploadStatus = HttpStatusCode.InsufficientStorage;

        var backup = Path.Combine(_dir, "snackbox_backup_20261024_030000_Daily.sql");
        await File.WriteAllTextAsync(backup, "dump");

        Assert.False(await _oneDrive.UploadAsync(backup));
        Assert.Contains("507", _oneDrive.GetStatus().LastError);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }

    /// <summary>The bits of login.microsoftonline.com and graph.microsoft.com the backup uses.</summary>
    private sealed class FakeMicrosoft : HttpMessageHandler
    {
        public bool SignedIn = true;
        public HttpStatusCode UploadStatus = HttpStatusCode.Created;
        public string? UploadedTo;
        public byte[]? Uploaded;
        public readonly List<string> Deleted = new();
        public readonly List<string> RefreshTokensUsed = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            var form = request.Content is FormUrlEncodedContent ? await request.Content.ReadAsStringAsync(cancellationToken) : "";

            if (url.EndsWith("/devicecode"))
                return Json("""{"device_code":"dc","user_code":"ABCD-EFGH","verification_uri":"https://microsoft.com/devicelogin","expires_in":900,"interval":5}""");

            if (url.EndsWith("/token") && form.Contains("device_code"))
                return SignedIn
                    ? Json("""{"access_token":"access-0","refresh_token":"refresh-1"}""")
                    : Json("""{"error":"authorization_pending"}""", HttpStatusCode.BadRequest);

            if (url.EndsWith("/token") && form.Contains("refresh_token"))
            {
                RefreshTokensUsed.Add(System.Web.HttpUtility.ParseQueryString(form)["refresh_token"]!);
                return Json("""{"access_token":"access-1","refresh_token":"refresh-2"}""");
            }

            if (url.Contains("/me/drive?"))
                return Json("""{"owner":{"user":{"displayName":"Jane","email":"jane@example.com"}}}""");

            if (request.Method == HttpMethod.Put)
            {
                UploadedTo = request.RequestUri.AbsolutePath;
                Uploaded = await request.Content!.ReadAsByteArrayAsync(cancellationToken);
                return new HttpResponseMessage(UploadStatus) { Content = new StringContent("{}") };
            }

            if (url.Contains(":/children"))
                return Json("""{"value":[{"id":"new","name":"snackbox_backup_20261024_030000_Daily.sql.enc"},{"id":"old-1","name":"snackbox_backup_20261022_030000_Daily.sql.enc"},{"id":"old-2","name":"snackbox_backup_20261023_030000_Daily.sql.enc"},{"id":"other","name":"holiday.jpg"}]}""");

            if (request.Method == HttpMethod.Delete)
            {
                Deleted.Add(url[(url.LastIndexOf('/') + 1)..]);
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
            new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
