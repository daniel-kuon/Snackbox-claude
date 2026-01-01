### Localization Guide

This app supports runtime language switching and user-specific language preferences.

Default language: English (en)
Supported languages: English (en), German (de)

Key features
- All user-facing strings are localizable via .resx resource files.
- Language can be switched at any time via the language selector (top bar and scanner page header).
- On barcode scan, the app automatically applies the stored language for that user (if available); otherwise it uses the globally selected language, falling back to English.

How it works
1) Resources
- Base resources live in: src/Snackbox.Components/Resources/SharedResources.resx
- German translations: src/Snackbox.Components/Resources/SharedResources.de.resx
- Marker class: src/Snackbox.Components/Resources/SharedResources.cs

2) Localization service
- Service: Snackbox.Components.Services.LocalizationService
- Contract: Snackbox.Components.Services.ILocalizationService
- Responsibilities:
  - Keeping current CultureInfo
  - Persisting preferences via IStorageService under keys:
    - preferred_language → global
    - preferred_language:{userId} → user specific
  - Raising OnCultureChanged event so UI re-renders

3) App bootstrap
- Blazor Server: src/Snackbox.BlazorServer/Program.cs
  - builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
  - Configures RequestLocalization with supported cultures ["en", "de"].
- .NET MAUI: src/Snackbox.Web/MauiProgram.cs
  - builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
  - Sets default thread culture to English.

4) Dynamic switching
- Top bar selector (Blazor Server): Components/Layout/NavMenu.razor
  - Switches global language immediately.
- Scanner page selector: Components/Pages/ScannerView.razor
  - When a session is active, switching persists preference for that specific user.
- On scan: ScannerService calls LocalizationService.ApplyUserLanguageAsync(userId) to apply the stored preference instantly.

Add a new language
1. Create a new resource file next to the base resources using the culture code suffix:
   - src/Snackbox.Components/Resources/SharedResources.fr.resx
2. Copy all keys from SharedResources.resx and provide translations in the new file.
3. Register the culture:
   - In LocalizationService._supported add new CultureInfo("fr").
   - In BlazorServer Program.cs add the culture to supportedCultures array passed to UseRequestLocalization.
   - In MAUI MauiProgram.cs you can also set default cultures if desired.
4. Rebuild and run. The new language will appear in the language selectors automatically.

Add new strings
1. Add the new key in SharedResources.resx with the English text.
2. Add the same key to all translated .resx files with translated text.
3. Use the string in components via: @inject IStringLocalizer<SharedResources> L and then L["Your.Key"].

Fallback behavior
- If a translation is missing in a specific .resx, the app will fall back to the base (English) value.

Notes
- Cultures are applied to both CurrentCulture and CurrentUICulture, so currency/date formatting also follows the selected language.
- To seed or modify a specific user’s language preference without the UI, set IStorageService keys directly:
  - preferred_language:{userId} = "de" (or any supported culture)
