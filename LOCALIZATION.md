# Localization Guide

This document provides a guide for adding and managing language support in the Snackbox application.

## Overview

Snackbox supports multiple languages with user-specific language preferences. The language setting is stored per user and automatically switches when a user logs in via barcode or password.

## Supported Languages

- **English (en)** - Default language
- **German (de)** - Secondary language

## Architecture

### Backend (API)
- **User Model**: Contains `PreferredLanguage` field (defaults to "en")
- **DTOs**: `UserDto`, `LoginResponse`, `ScanBarcodeResponse` all include `PreferredLanguage`
- **API Endpoint**: `PATCH /api/users/{id}/language` to update user's language preference

### Frontend (Blazor Components)
- **Resource Files**: `.resx` files in `src/Snackbox.Components/Resources/`
  - `SharedResources.resx` - English (default)
  - `SharedResources.de.resx` - German
- **LocalizationService**: Manages culture switching
- **IStringLocalizer**: Used in Razor components for localized strings

## How Localization Works

1. **User Login/Scan**: When a user logs in or scans their barcode, the API returns their `PreferredLanguage`
2. **Culture Switch**: `LocalizationService.SetCulture()` is called with the user's preferred language
3. **UI Update**: Razor components using `IStringLocalizer<SharedResources>` automatically display text in the selected language

## Adding a New Language

### Step 1: Add Resource File

1. Navigate to `src/Snackbox.Components/Resources/`
2. Create a new resource file: `SharedResources.{language-code}.resx`
   - Example: `SharedResources.fr.resx` for French
   - Example: `SharedResources.es.resx` for Spanish

3. Copy the structure from `SharedResources.resx`
4. Translate all values to the new language

**Example Entry:**
```xml
<data name="Login.Title" xml:space="preserve">
  <value>Connexion Snackbox</value>  <!-- French translation -->
</data>
```

### Step 2: Update Project File

Add the new resource file to `src/Snackbox.Components/Snackbox.Components.csproj`:

```xml
<ItemGroup>
  <EmbeddedResource Update="Resources\SharedResources.fr.resx">
    <DependentUpon>SharedResources.resx</DependentUpon>
  </EmbeddedResource>
</ItemGroup>
```

### Step 3: Update API Validation

Update the language validation in `src/Snackbox.Api/Controllers/UsersController.cs`:

```csharp
[HttpPatch("{id}/language")]
public async Task<ActionResult> UpdateLanguage(int id, [FromBody] UpdateLanguageDto dto)
{
    var user = await _context.Users.FindAsync(id);

    if (user == null)
    {
        return NotFound(new { message = "User not found" });
    }

    // Update validation to include new language
    if (dto.PreferredLanguage != "en" && 
        dto.PreferredLanguage != "de" && 
        dto.PreferredLanguage != "fr")  // Add new language here
    {
        return BadRequest(new { message = "Invalid language code. Supported languages: en, de, fr" });
    }

    user.PreferredLanguage = dto.PreferredLanguage;
    await _context.SaveChangesAsync();

    _logger.LogInformation("User language updated: {UserId} - {Username} - Language: {Language}", 
        user.Id, user.Username, user.PreferredLanguage);

    return Ok(new { message = "Language preference updated successfully", preferredLanguage = user.PreferredLanguage });
}
```

### Step 4: Test

1. **Build the project**: `dotnet build`
2. **Run the application**
3. **Update a user's language preference** using the API or database
4. **Login with that user** and verify the UI displays in the correct language

## Using Localization in Razor Components

### Step 1: Inject Dependencies

```razor
@using Microsoft.Extensions.Localization
@using Snackbox.Components.Resources
@inject IStringLocalizer<SharedResources> Localizer
```

### Step 2: Use Localized Strings

```razor
<h2>@Localizer["Login.Title"]</h2>
<label>@Localizer["Login.Username"]</label>
<button>@Localizer["Login.LoginButton"]</button>
```

### Step 3: Localized Strings with Parameters

```razor
@Localizer["PasswordSetup.Welcome", username]
```

The resource file entry should use `{0}` for the parameter:
```xml
<data name="PasswordSetup.Welcome" xml:space="preserve">
  <value>Welcome {0}! You don't have a password set yet.</value>
</data>
```

## Resource File Naming Convention

All resource keys follow a hierarchical naming pattern:

```
{Page/Feature}.{Component/Element}.{Property}
```

**Examples:**
- `Login.Title` - Login page title
- `Login.Username` - Username label on login page
- `Scanner.Balance` - Balance label on scanner view
- `Admin.Users.Title` - User management page title
- `Common.Save` - Common save button text

## Language Switching Behavior

### Automatic Switching
- **On Login**: User's preferred language is applied when they login with password
- **On Barcode Scan**: User's preferred language is applied when they scan their barcode
- **Persistent**: Language persists throughout the session

### Manual Switching (Future Enhancement)
To add a language selector UI:

1. Create a language selector component
2. Call `LocalizationService.SetCulture(languageCode)`
3. Optionally: Update user preference via API call to persist the change

## Testing Localization

### Manual Testing
1. Create test users with different `PreferredLanguage` values:
   ```sql
   UPDATE users SET preferred_language = 'de' WHERE username = 'testuser';
   ```

2. Login with each user and verify UI language

### Automated Testing
Consider adding tests for:
- Resource file completeness (all keys present in all languages)
- Culture switching behavior
- Fallback to English when translation missing

## Fallback Behavior

If a translation is missing in a specific language:
1. .NET localization automatically falls back to the default resource file (English)
2. The key name is displayed if no translation exists in any resource file

## Best Practices

1. **Keep Keys Organized**: Follow the naming convention
2. **Complete Translations**: Ensure all languages have all keys
3. **Use Meaningful Keys**: Make keys descriptive (`Login.Username` not `LBL_01`)
4. **Test Translations**: Have native speakers review translations
5. **Document New Keys**: Update this guide when adding new resource keys

## Common Issues and Solutions

### Issue: Translations Not Showing
**Solution**: 
- Ensure resource file is marked as `EmbeddedResource` in project file
- Verify resource key exists in all language files
- Check that culture is being set correctly

### Issue: Resource File Not Found
**Solution**:
- Verify the resource file name follows the pattern `SharedResources.{code}.resx`
- Ensure file is in `Resources` directory
- Rebuild the project

### Issue: Language Not Persisting
**Solution**:
- Verify user's `PreferredLanguage` is saved in database
- Check that `LocalizationService.SetCulture()` is called on login
- Ensure API returns `PreferredLanguage` in login/scan responses

## Future Enhancements

- [ ] Add language selector in user profile settings
- [ ] Add language selector for non-authenticated users (login page)
- [ ] Add RTL (Right-to-Left) language support (Arabic, Hebrew)
- [ ] Add date/time localization
- [ ] Add number/currency formatting per locale
- [ ] Add pluralization support for dynamic text
- [ ] Add missing translations validation in CI/CD

## Additional Resources

- [.NET Globalization and Localization](https://learn.microsoft.com/en-us/dotnet/core/extensions/globalization-and-localization)
- [Blazor Globalization and Localization](https://learn.microsoft.com/en-us/aspnet/core/blazor/globalization-localization)
- [ISO 639-1 Language Codes](https://en.wikipedia.org/wiki/List_of_ISO_639-1_codes)
