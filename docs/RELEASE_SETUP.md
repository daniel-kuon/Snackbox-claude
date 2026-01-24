# Release Setup Guide

Before creating your first release, update the following placeholders with your actual GitHub information.

## Files to Update

### 1. README.md
**Location**: `README.md`

Replace:
```markdown
YOUR_GITHUB_USERNAME
```

With your actual GitHub username (e.g., `john-doe`)

**Lines to update**:
- Line 30: Installation command
- Line 40: Alternative installation
- Line 48: Manual download link
- Multiple references throughout

---

### 2. install-snackbox.ps1
**Location**: `install-snackbox.ps1`

Update parameters at the top:
```powershell
param(
    [string]$RepoOwner = "YOUR_GITHUB_USERNAME",  # ← Update this
    [string]$RepoName = "snackbox-claude",
    ...
)
```

---

### 3. Snackbox.Updater Program.cs
**Location**: `tools/Snackbox.Updater/Program.cs`

Update around line 12-13:
```csharp
var repoOwner = "YOUR_GITHUB_USERNAME"; // ← Update this
var repoName = "snackbox-claude";      // Update if you renamed the repo
```

---

### 4. Snackbox.Updater UpdateManager.cs
**Location**: `tools/Snackbox.Updater/UpdateManager.cs`

Update line 124 (checksum URL):
```csharp
var checksumUrl = $"https://github.com/YOUR_OWNER/YOUR_REPO/releases/latest/download/checksums.txt";
```

Change to:
```csharp
var checksumUrl = $"https://github.com/YOUR_GITHUB_USERNAME/snackbox-claude/releases/latest/download/checksums.txt";
```

---

### 5. docs/INSTALLATION.md
**Location**: `docs/INSTALLATION.md`

Replace all occurrences of:
```
YOUR_GITHUB_USERNAME
```

With your actual GitHub username

---

## Creating Your First Release

### Option 1: Using Git Tags (Recommended)

```bash
# Update version in Directory.Build.props if needed
# Commit all changes
git add .
git commit -m "Prepare v1.0.0 release"

# Create and push version tag
git tag v1.0.0
git push origin v1.0.0
```

The GitHub Action will automatically:
- Build all projects
- Create release packages
- Generate checksums
- Create GitHub release with artifacts

### Option 2: Manual Workflow Dispatch

1. Go to GitHub: **Actions** → **Release**
2. Click **Run workflow**
3. Enter version number (e.g., `1.0.0`)
4. Click **Run workflow**

---

## Verifying the Release

After the GitHub Action completes:

1. Go to **Releases** in your GitHub repository
2. Verify these artifacts are present:
   - `snackbox-full-{version}-win-x64.zip`
   - `snackbox-updater-{version}-win-x64.zip`
   - `checksums.txt`
3. Check that release notes were auto-generated
4. Test the installation command:
   ```powershell
   irm https://raw.githubusercontent.com/YOUR_GITHUB_USERNAME/snackbox-claude/main/install-snackbox.ps1 | iex
   ```

---

## Version Numbering

Follow [Semantic Versioning](https://semver.org/):

- **Major** (1.0.0 → 2.0.0): Breaking changes
- **Minor** (1.0.0 → 1.1.0): New features, backward compatible
- **Patch** (1.0.0 → 1.0.1): Bug fixes

Update version in `Directory.Build.props`:
```xml
<Version>1.0.0</Version>
```

---

## Troubleshooting

### GitHub Action Fails

**Check**:
- All placeholders are updated
- Repository has correct permissions
- .NET 10 SDK is available
- All dependencies are restorable

### Release Artifacts Missing

**Check**:
- GitHub Action completed successfully
- `release.yml` workflow has correct permissions
- Release was not marked as draft

### Installer Fails to Download

**Check**:
- Repository is public (or token authentication is configured)
- Release is published (not draft)
- Asset names match expected patterns

---

## Next Steps

1. ✅ Update all placeholders
2. ✅ Test build locally: `dotnet build -c Release`
3. ✅ Commit changes
4. ✅ Create first release tag
5. ✅ Wait for GitHub Action to complete
6. ✅ Test installation command
7. ✅ Document any custom configuration

---

## Quick Checklist

Before creating a release:

- [ ] Updated `YOUR_GITHUB_USERNAME` in README.md
- [ ] Updated `RepoOwner` in install-snackbox.ps1
- [ ] Updated `repoOwner` in Snackbox.Updater/Program.cs
- [ ] Updated checksum URL in UpdateManager.cs
- [ ] Updated docs/INSTALLATION.md
- [ ] Tested build: `dotnet build -c Release`
- [ ] Committed all changes
- [ ] Ready to create tag and release

Once complete, you're ready to create your first release! 🚀
