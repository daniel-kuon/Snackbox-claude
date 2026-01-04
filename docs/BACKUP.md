# Backup and Restore Documentation

## Overview

The Snackbox application includes a comprehensive backup and restore system that allows you to:
- Create manual backups of your database
- Automatically create daily, weekly, and monthly backups
- Restore from any backup
- Import/export backups
- Receive weekly backup emails
- Automatically clean up old backups

## Prerequisites

### PostgreSQL Tools

The backup functionality requires PostgreSQL client tools (`pg_dump` and `psql`) to be installed on the system:

**Ubuntu/Debian:**
```bash
sudo apt-get update
sudo apt-get install postgresql-client
```

**Windows:**
- Install from [PostgreSQL Downloads](https://www.postgresql.org/download/windows/)
- Ensure the PostgreSQL `bin` directory is in your PATH

**macOS:**
```bash
brew install postgresql
```

### Configuration

Add the following settings to `appsettings.json`:

```json
{
  "Backup": {
    "Directory": "backups"
  },
  "Email": {
    "SmtpHost": "smtp.example.com",
    "SmtpPort": "587",
    "SmtpUsername": "your-email@example.com",
    "SmtpPassword": "your-password",
    "FromEmail": "snackbox@example.com",
    "BackupRecipient": "admin@example.com",
    "EnableSsl": "true"
  }
}
```

**Configuration Options:**

- `Backup:Directory`: Directory where backups are stored (relative or absolute path)
- `Email:SmtpHost`: SMTP server hostname
- `Email:SmtpPort`: SMTP server port (usually 587 for TLS)
- `Email:SmtpUsername`: SMTP authentication username
- `Email:SmtpPassword`: SMTP authentication password
- `Email:FromEmail`: Email address to send from
- `Email:BackupRecipient`: Email address to receive weekly backups
- `Email:EnableSsl`: Enable SSL/TLS for SMTP connection

## Features

### 1. Manual Backups

Create a backup at any time through the admin interface:

1. Navigate to **Admin > Backups**
2. Click **Create Backup** button
3. The backup will be created and listed in the table

Manual backups are marked as "Manual" type and are not automatically deleted.

### 2. Automatic Backups

The system automatically creates backups daily at midnight (UTC):

- **Daily Backups**: Created every day, kept for 30 days
- **Weekly Backups**: Created every Sunday, kept for 90 days
- **Monthly Backups**: Created on the 1st of each month, kept forever

### 3. Weekly Email Backups

Once per week (on Sunday), the latest weekly backup is automatically sent to the configured email address (`Email:BackupRecipient`).

### 4. Backup Cleanup

Old backups are automatically cleaned up based on their type:

- Daily backups older than 30 days are deleted
- Weekly backups older than 90 days are deleted
- Monthly backups are never deleted
- Manual backups are never deleted

### 5. Restore from Backup

To restore a backup:

1. Navigate to **Admin > Backups**
2. Find the backup you want to restore
3. Click the **Restore** button
4. Confirm the action
5. The database will be completely replaced with the backup data
6. The application will reload automatically

**⚠️ Warning:** Restoring a backup will completely replace your current database. All existing data will be lost.

### 6. Import/Export Backups

**Import a Backup:**
1. Navigate to **Admin > Backups**
2. Click **Import Backup**
3. Select a `.sql` backup file from your computer
4. The backup will be imported and added to the list

**Export/Download a Backup:**
1. Navigate to **Admin > Backups**
2. Find the backup you want to download
3. Click the **Download** button
4. Save the `.sql` file to your computer

### 7. Delete Backups

To manually delete a backup:

1. Navigate to **Admin > Backups**
2. Find the backup you want to delete
3. Click the **Delete** button
4. Confirm the action

## Database Setup

If the database is not available when the application starts, you can set it up using the Database Setup page:

Navigate to `/database-setup` and choose one of the following options:

### 1. Restore from Backup
- Upload a previously saved backup file
- The database will be created and restored from the backup
- Use this when migrating to a new server or recovering from a failure

### 2. Create Empty Database
- Creates a fresh database with no data
- Includes all tables and schema
- You'll need to create users and products manually
- Use this for a clean start without sample data

### 3. Create with Sample Data
- Creates a database with sample users, products, and transactions
- Includes demo data for testing
- Default admin user: `admin` / `adminPassword`
- Use this for testing or initial setup

## API Endpoints

The backup functionality exposes the following API endpoints:

### Backup Management

- `POST /api/backup/create` - Create a manual backup
- `GET /api/backup/list` - List all available backups
- `POST /api/backup/restore/{id}` - Restore a backup by ID
- `POST /api/backup/import` - Import a backup file
- `GET /api/backup/download/{id}` - Download a backup file
- `DELETE /api/backup/{id}` - Delete a backup

### Database Management

- `GET /api/backup/database/check` - Check if database exists
- `POST /api/backup/database/create-empty` - Create empty database
- `POST /api/backup/database/create-seeded` - Create database with sample data

## Backup File Format

Backups are stored as PostgreSQL SQL dump files (`.sql`) with the following naming convention:

```
snackbox_backup_YYYYMMDD_HHmmss_<type>.sql
```

Example: `snackbox_backup_20260104_120000_Daily.sql`

## Backup Metadata

Backup metadata is stored in `backups/metadata.json` with the following information:

```json
{
  "Id": "20260104_120000_Daily",
  "FileName": "snackbox_backup_20260104_120000_Daily.sql",
  "CreatedAt": "2026-01-04T12:00:00Z",
  "Type": "Daily",
  "FileSizeBytes": 1048576
}
```

## Troubleshooting

### "pg_dump not found" Error

Ensure PostgreSQL client tools are installed and in your system PATH.

### Backup Creation Fails

1. Check database connection string in `appsettings.json`
2. Verify the backup directory exists and is writable
3. Check logs for detailed error messages

### Email Sending Fails

1. Verify SMTP settings in `appsettings.json`
2. Check that your SMTP credentials are correct
3. Ensure firewall allows outbound SMTP connections
4. Check logs for detailed error messages

### Restore Fails

1. Ensure the backup file is a valid PostgreSQL dump
2. Check that you have sufficient permissions
3. Verify no other connections are using the database
4. Check logs for detailed error messages

## Security Considerations

1. **Backup Files**: Contain all database data including passwords (hashed). Store securely.
2. **Email Backups**: Sent via email, ensure secure email transmission
3. **Access Control**: Only admin users can access backup functionality
4. **File Permissions**: Ensure backup directory has appropriate permissions

## Best Practices

1. **Regular Testing**: Periodically test restore functionality to ensure backups are valid
2. **Off-site Storage**: Download and store critical backups off-site
3. **Monitoring**: Monitor backup logs to ensure automatic backups are running
4. **Retention Policy**: Adjust retention settings based on your requirements
5. **Email Configuration**: Set up email backups for off-site storage
6. **Backup Before Updates**: Always create a manual backup before major updates

## Example Workflow

### Setting Up a New Server

1. Install Snackbox on the new server
2. Start the application
3. Navigate to `/database-setup`
4. Choose "Restore from Backup"
5. Upload your backup file
6. Wait for restore to complete
7. Login with your existing credentials

### Regular Maintenance

1. Check the Backups page weekly
2. Verify automatic backups are being created
3. Download important backups for off-site storage
4. Delete any manual test backups no longer needed
5. Verify weekly email backups are being received

### Recovery from Failure

1. Start the application
2. Navigate to Admin > Backups (or /database-setup if database is missing)
3. Restore from the most recent backup
4. Verify data integrity
5. Resume normal operations
