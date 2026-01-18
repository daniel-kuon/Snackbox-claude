using System.Net;
using System.Net.Mail;
using System.Text.Json;
using Snackbox.Api.Dtos;

namespace Snackbox.Api.Services;

public class SettingsService : ISettingsService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SettingsService> _logger;
    private readonly string _secretsFilePath;

    public SettingsService(IConfiguration configuration, ILogger<SettingsService> logger, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _logger = logger;
        _secretsFilePath = Path.Combine(environment.ContentRootPath, "appsettings.secrets.json");
    }

    public ApplicationSettingsDto GetSettings()
    {
        var settings = new ApplicationSettingsDto
        {
            EmailSettings = new EmailSettingsDto
            {
                Enabled = _configuration.GetValue<bool>("EmailSettings:Enabled"),
                SmtpServer = _configuration["EmailSettings:SmtpServer"] ?? "",
                SmtpPort = _configuration.GetValue<int>("EmailSettings:SmtpPort"),
                EnableSsl = _configuration.GetValue<bool>("EmailSettings:EnableSsl"),
                Username = _configuration["EmailSettings:Username"] ?? "",
                Password = _configuration["EmailSettings:Password"] ?? "",
                FromEmail = _configuration["EmailSettings:FromEmail"] ?? "",
                FromName = _configuration["EmailSettings:FromName"] ?? "",
                PayPalLink = _configuration["EmailSettings:PayPalLink"] ?? ""
            },
            BackupSettings = new BackupSettingsDto
            {
                Directory = _configuration["Backup:Directory"] ?? "",
                EmailRecipient = _configuration["Backup:EmailRecipient"] ?? ""
            },
            BarcodeLookupSettings = new BarcodeLookupSettingsDto
            {
                ApiKey = _configuration["SearchUpcData:ApiKey"] ?? ""
            }
        };

        return settings;
    }

    public async Task<bool> UpdateSettingsAsync(ApplicationSettingsDto settings)
    {
        try
        {
            // Create secrets file if it doesn't exist
            if (!File.Exists(_secretsFilePath))
            {
                _logger.LogInformation("Secrets file not found, creating new one at: {Path}", _secretsFilePath);
                await CreateDefaultSecretsFileAsync();
            }

            // Write directly to secrets file
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

            writer.WriteStartObject();

            // EmailSettings
            writer.WritePropertyName("EmailSettings");
            writer.WriteStartObject();
            writer.WriteBoolean("Enabled", settings.EmailSettings.Enabled);
            writer.WriteString("SmtpServer", settings.EmailSettings.SmtpServer);
            writer.WriteNumber("SmtpPort", settings.EmailSettings.SmtpPort);
            writer.WriteBoolean("EnableSsl", settings.EmailSettings.EnableSsl);
            writer.WriteString("Username", settings.EmailSettings.Username);
            writer.WriteString("Password", settings.EmailSettings.Password);
            writer.WriteString("FromEmail", settings.EmailSettings.FromEmail);
            writer.WriteString("FromName", settings.EmailSettings.FromName);
            writer.WriteString("PayPalLink", settings.EmailSettings.PayPalLink);
            writer.WriteEndObject();

            // Backup
            writer.WritePropertyName("Backup");
            writer.WriteStartObject();
            writer.WriteString("Directory", settings.BackupSettings.Directory);
            writer.WriteString("EmailRecipient", settings.BackupSettings.EmailRecipient);
            writer.WriteEndObject();

            // SearchUpcData
            writer.WritePropertyName("SearchUpcData");
            writer.WriteStartObject();
            writer.WriteString("ApiKey", settings.BarcodeLookupSettings.ApiKey);
            writer.WriteEndObject();

            writer.WriteEndObject();
            await writer.FlushAsync();

            var updatedJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            await File.WriteAllTextAsync(_secretsFilePath, updatedJson);

            _logger.LogInformation("Settings updated successfully in secrets file");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings");
            return false;
        }
    }

    private async Task CreateDefaultSecretsFileAsync()
    {
        var defaultSettings = new
        {
            EmailSettings = new
            {
                Enabled = false,
                SmtpServer = "smtp.gmail.com",
                SmtpPort = 587,
                EnableSsl = true,
                Username = "",
                Password = "",
                FromEmail = "noreply@snackbox.example.com",
                FromName = "Snackbox",
                PayPalLink = "https://paypal.me/yourpaypallink"
            },
            Backup = new
            {
                Directory = "backups",
                EmailRecipient = "admin@example.com"
            },
            SearchUpcData = new
            {
                ApiKey = "YOUR_API_KEY_HERE"
            }
        };

        var json = JsonSerializer.Serialize(defaultSettings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_secretsFilePath, json);
    }

    public async Task<TestResultDto> TestEmailSettingsAsync(EmailSettingsDto settings)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(settings.SmtpServer))
            {
                return new TestResultDto { Success = false, Message = "SMTP server is required" };
            }

            if (string.IsNullOrWhiteSpace(settings.FromEmail))
            {
                return new TestResultDto { Success = false, Message = "From email is required" };
            }

            using var client = new SmtpClient(settings.SmtpServer, settings.SmtpPort)
            {
                EnableSsl = settings.EnableSsl,
                Credentials = new NetworkCredential(settings.Username, settings.Password)
            };

            var message = new MailMessage
            {
                From = new MailAddress(settings.FromEmail, settings.FromName),
                Subject = "Snackbox - Email Test",
                Body = "This is a test email from Snackbox settings configuration.",
                IsBodyHtml = false
            };
            message.To.Add(settings.FromEmail);

            await client.SendMailAsync(message);

            _logger.LogInformation("Email test successful");
            return new TestResultDto { Success = true, Message = "Email sent successfully! Check your inbox." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email test failed");
            return new TestResultDto { Success = false, Message = $"Email test failed: {ex.Message}" };
        }
    }

    public async Task<TestResultDto> TestBarcodeLookupAsync(BarcodeLookupSettingsDto settings)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(settings.ApiKey) || settings.ApiKey == "YOUR_API_KEY_HERE")
            {
                return new TestResultDto { Success = false, Message = "Valid API key is required" };
            }

            using var httpClient = new HttpClient();
            var testBarcode = "049000050103"; // Coca-Cola test barcode
            var url = $"https://searchupcdata.com/api/products/{testBarcode}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {settings.ApiKey}");

            var response = await httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                return new TestResultDto { Success = false, Message = "Invalid API key" };
            }

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Barcode lookup test successful");
                return new TestResultDto { Success = true, Message = "API key is valid and working!" };
            }

            return new TestResultDto { Success = false, Message = $"API returned status: {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Barcode lookup test failed");
            return new TestResultDto { Success = false, Message = $"Test failed: {ex.Message}" };
        }
    }

    public async Task<TestResultDto> TestBackupSettingsAsync(BackupSettingsDto settings)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(settings.Directory))
            {
                return new TestResultDto { Success = false, Message = "Backup directory is required" };
            }

            var fullPath = Path.IsPathRooted(settings.Directory)
                ? settings.Directory
                : Path.Combine(Directory.GetCurrentDirectory(), settings.Directory);

            // Check if directory exists, if not try to create it
            if (!Directory.Exists(fullPath))
            {
                try
                {
                    Directory.CreateDirectory(fullPath);
                }
                catch (Exception ex)
                {
                    return new TestResultDto { Success = false, Message = $"Cannot create directory: {ex.Message}" };
                }
            }

            // Test write permissions by creating a test file
            var testFile = Path.Combine(fullPath, $"test_{DateTime.UtcNow.Ticks}.tmp");
            try
            {
                await File.WriteAllTextAsync(testFile, "test");
                File.Delete(testFile);
            }
            catch (Exception ex)
            {
                return new TestResultDto { Success = false, Message = $"Cannot write to directory: {ex.Message}" };
            }

            _logger.LogInformation("Backup settings test successful");
            return new TestResultDto { Success = true, Message = $"Backup directory is accessible and writable at: {fullPath}" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup settings test failed");
            return new TestResultDto { Success = false, Message = $"Test failed: {ex.Message}" };
        }
    }
}
