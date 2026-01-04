# Backup Configuration Example

# This file provides example configurations for the Snackbox backup system.
# Copy the relevant sections to your appsettings.json file.

# Basic Backup Configuration
# Minimal configuration for local backups only
{
  "Backup": {
    "Directory": "backups"  # Relative path from application directory
  }
}

# Email Configuration Examples

## Gmail Example
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",  # Use App Password, not regular password
    "FromEmail": "your-email@gmail.com",
    "BackupRecipient": "backup-recipient@example.com",
    "EnableSsl": "true"
  }
}

## Outlook/Office 365 Example
{
  "Email": {
    "SmtpHost": "smtp.office365.com",
    "SmtpPort": "587",
    "SmtpUsername": "your-email@outlook.com",
    "SmtpPassword": "your-password",
    "FromEmail": "your-email@outlook.com",
    "BackupRecipient": "backup-recipient@example.com",
    "EnableSsl": "true"
  }
}

## Custom SMTP Server Example
{
  "Email": {
    "SmtpHost": "mail.example.com",
    "SmtpPort": "587",
    "SmtpUsername": "snackbox@example.com",
    "SmtpPassword": "secure-password",
    "FromEmail": "snackbox@example.com",
    "BackupRecipient": "admin@example.com",
    "EnableSsl": "true"
  }
}

## SendGrid Example
{
  "Email": {
    "SmtpHost": "smtp.sendgrid.net",
    "SmtpPort": "587",
    "SmtpUsername": "apikey",  # Literally "apikey"
    "SmtpPassword": "YOUR_SENDGRID_API_KEY",
    "FromEmail": "verified-sender@example.com",
    "BackupRecipient": "backup-recipient@example.com",
    "EnableSsl": "true"
  }
}

# Complete Configuration Example
{
  "ConnectionStrings": {
    "snackboxdb": "Host=localhost;Port=5432;Database=snackboxdb;Username=postgres;Password=your-password"
  },
  "Backup": {
    "Directory": "/var/backups/snackbox"  # Absolute path for production
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUsername": "snackbox@example.com",
    "SmtpPassword": "your-app-password",
    "FromEmail": "snackbox@example.com",
    "BackupRecipient": "admin@example.com",
    "EnableSsl": "true"
  },
  "JwtSettings": {
    "SecretKey": "YourVerySecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "SnackboxApi",
    "Audience": "SnackboxClient",
    "ExpirationMinutes": "60"
  }
}

# Notes:

## Gmail Setup
# 1. Enable 2-factor authentication on your Google account
# 2. Generate an App Password: https://myaccount.google.com/apppasswords
# 3. Use the App Password (not your regular password) in SmtpPassword

## Backup Directory
# - Use relative paths for development (e.g., "backups")
# - Use absolute paths for production (e.g., "/var/backups/snackbox")
# - Ensure the directory is writable by the application user
# - Consider using a separate volume/partition for backups

## Email Testing
# To test email configuration without waiting for automatic backups:
# 1. Navigate to Admin > Backups
# 2. Create a manual backup
# 3. Check application logs for email-related errors

## Security
# - Never commit appsettings.json with real credentials to source control
# - Use environment variables or secrets management for production
# - Ensure backup directory has restricted permissions
# - Regularly rotate SMTP credentials

## Troubleshooting
# If backups aren't working:
# 1. Check that pg_dump and psql are installed and in PATH
# 2. Verify database connection string is correct
# 3. Ensure backup directory exists and is writable
# 4. Check application logs for detailed error messages

# If emails aren't sending:
# 1. Test SMTP credentials using a tool like telnet or swaks
# 2. Check firewall allows outbound connections on SMTP port
# 3. Verify FromEmail is authorized to send via the SMTP server
# 4. Check application logs for SMTP errors
