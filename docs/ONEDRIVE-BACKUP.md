# Encrypted backups on OneDrive

Every automatic (daily) backup is encrypted on the Snackbox PC and uploaded to the folder
`Snackbox-Backups` of a OneDrive you connect. OneDrive keeps the newest 30
(`Backup:OneDrive:Keep`). Local backups are unchanged.

## 1. App registration (once, about 5 minutes)

Microsoft only lets registered applications sign in, and there is no shared registration for
self-hosted apps like this one, so Snackbox needs one of yours. It costs nothing.

1. Open <https://portal.azure.com> → **Microsoft Entra ID** → **App registrations** → **New registration**
   (or <https://entra.microsoft.com> → Applications → App registrations).
2. Name: `Snackbox Backup`. Supported account types: **Accounts in any organizational directory
   and personal Microsoft accounts** - works for a personal and for a work OneDrive. No redirect
   URI. **Register**.
   (If your company only allows *this organizational directory only*, that works too: put the
   *Directory (tenant) ID* into `Backup:OneDrive:Tenant` in the API's `appsettings.secrets.json`.)
3. **Authentication** → *Advanced settings* → **Allow public client flows: Yes** → Save.
   (Snackbox signs in with a code you type on another device; that is a "public client".)
4. **API permissions** → Add a permission → Microsoft Graph → *Delegated* → `Files.ReadWrite`
   and `offline_access` → Add. (A work tenant may need an admin to grant consent.)
5. Copy the **Application (client) ID** from the *Overview* page.

## 2. Connect (Admin → Backups → OneDrive)

1. Paste the client id, choose a **passphrase** (at least 12 characters), **Save**.
   **Put the passphrase in your password manager.** Without it the uploaded files cannot be
   opened, and the copy on the Snackbox PC is stored so that only that PC can read it - if the
   PC dies, the passphrase dies with it.
2. **Connect OneDrive** shows a code. Open the link on any device (your phone is fine), enter the
   code and sign in with the account whose OneDrive should hold the backups.
3. **Upload the newest backup now** tries it out. The page shows the last upload or the last error.

## Restoring from OneDrive

Download the `.sql.enc` file from OneDrive, then Admin → Backups → **Import Backup** and pick it
(on a fresh machine the database setup page offers the same). If the passphrase on this machine is
not the one it was encrypted with, the page asks for it.

## What is protected how

- **Backup files**: AES-256-GCM, key from the passphrase via PBKDF2-SHA256 (600 000 rounds).
  Microsoft only ever sees the encrypted file. A wrong passphrase or a changed file fails to
  decrypt rather than giving broken SQL.
- **On the PC**: the OneDrive sign-in (refresh token) and the passphrase are kept in
  `%ProgramData%\Snackbox\onedrive.json`, encrypted with ASP.NET Data Protection (Windows DPAPI,
  user scope) - unreadable on any other machine or for another Windows user.
- The permission is `Files.ReadWrite` (the whole OneDrive) rather than an app-only folder, because
  the app folder is not available on every work OneDrive. Use a OneDrive that is meant for this if
  that matters to you; **Disconnect** forgets the sign-in on the PC.
