namespace Snackbox.Api.Dtos;

public class EmailSettingsDto
{
    public bool Enabled { get; set; }
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public bool EnableSsl { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string PayPalLink { get; set; } = string.Empty;
}

public class BackupSettingsDto
{
    public string Directory { get; set; } = string.Empty;
    public string EmailRecipient { get; set; } = string.Empty;
}

public class BarcodeLookupSettingsDto
{
    public string ApiKey { get; set; } = string.Empty;
}

public class ApplicationSettingsDto
{
    public EmailSettingsDto EmailSettings { get; set; } = new();
    public BackupSettingsDto BackupSettings { get; set; } = new();
    public BarcodeLookupSettingsDto BarcodeLookupSettings { get; set; } = new();
}

public class TestResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
