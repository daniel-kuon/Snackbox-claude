# Backup Configuration Example

# This file provides example configurations for the Snackbox backup system.
# Copy the relevant sections to your appsettings.json file.

# Basic Backup Configuration
# Minimal configuration for local backups only
{
  "Backup": {
    "Directory": "backups",  # Relative path from application directory
    "EmailRecipient": "admin@example.com"  # Optional: recipient for weekly backup emails
  }
}

# Email Configuration Examples
# Note: EmailSettings is a unified configuration used for both backup emails and payment reminders

## Gmail Example
{
  "EmailSettings": {
    "Enabled": true,
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "EnableSsl": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",  # Use App Password, not regular password
    "FromEmail": "your-email@gmail.com",
    "FromName": "Snackbox",
    "PayPalLink": "https://paypal.me/yourpaypallink"  # Optional: for payment reminders
  }
}

## Outlook/Office 365 Example
{
  "EmailSettings": {
    "Enabled": true,
    "SmtpServer": "smtp.office365.com",
    "SmtpPort": 587,
    "EnableSsl": true,
    "Username": "your-email@outlook.com",
    "Password": "your-password",
    "FromEmail": "your-email@outlook.com",
    "FromName": "Snackbox",
    "PayPalLink": "https://paypal.me/yourpaypallink"
  }
}

## Custom SMTP Server Example
{
  "EmailSettings": {
    "Enabled": true,
    "SmtpServer": "mail.example.com",
    "SmtpPort": 587,
    "EnableSsl": true,
    "Username": "snackbox@example.com",
    "Password": "secure-password",
    "FromEmail": "snackbox@example.com",
    "FromName": "Snackbox System",
    "PayPalLink": ""
  }
}

## SendGrid Example
{
  "EmailSettings": {
    "Enabled": true,
    "SmtpServer": "smtp.sendgrid.net",
    "SmtpPort": 587,
    "EnableSsl": true,
    "Username": "apikey",  # Literally "apikey"
    "Password": "YOUR_SENDGRID_API_KEY",
    "FromEmail": "verified-sender@example.com",
    "FromName": "Snackbox",
    "PayPalLink": ""
  }
}

# Complete Configuration Example
{
  "ConnectionStrings": {
    "snackboxdb": "Host=localhost;Port=5432;Database=snackboxdb;Username=postgres;Password=your-password"
  },
  "Backup": {
    "Directory": "/var/backups/snackbox",  # Absolute path for production
    "EmailRecipient": "admin@example.com"  # Recipient for weekly backup emails
  },
  "EmailSettings": {
    "Enabled": true,
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "EnableSsl": true,
    "Username": "snackbox@example.com",
    "Password": "your-app-password",
    "FromEmail": "snackbox@example.com",
    "FromName": "Snackbox",
    "PayPalLink": "https://paypal.me/yourpaypallink"
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
