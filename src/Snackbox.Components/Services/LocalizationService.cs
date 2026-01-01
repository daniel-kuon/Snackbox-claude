using System.Globalization;

namespace Snackbox.Components.Services;

public class LocalizationService : ILocalizationService
{
    private readonly IStorageService _storage;
    private static readonly CultureInfo DefaultCulture = new("en");
    private static readonly CultureInfo[] _supported = new[] { new CultureInfo("en"), new CultureInfo("de") };

    public LocalizationService(IStorageService storage)
    {
        _storage = storage;
        CurrentCulture = CultureInfo.CurrentUICulture;
        if (!_supported.Any(c => string.Equals(c.Name, CurrentCulture.Name, StringComparison.OrdinalIgnoreCase)))
        {
            CurrentCulture = DefaultCulture;
            ApplyCulture(CurrentCulture);
        }
    }

    public CultureInfo CurrentCulture { get; private set; }
    public IReadOnlyList<CultureInfo> SupportedCultures => _supported;

    public event Action<CultureInfo>? OnCultureChanged;

    public async Task SetCultureAsync(string culture, string? userId = null, bool persist = true)
    {
        var target = _supported.FirstOrDefault(c => string.Equals(c.Name, culture, StringComparison.OrdinalIgnoreCase))
                    ?? DefaultCulture;

        CurrentCulture = target;
        ApplyCulture(target);

        if (persist)
        {
            await _storage.SetAsync("preferred_language", target.Name);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                await _storage.SetAsync($"preferred_language:{userId}", target.Name);
            }
        }

        OnCultureChanged?.Invoke(CurrentCulture);
    }

    public async Task ApplyUserLanguageAsync(string userId)
    {
        // Try user-specific preference first, then global preference
        var userKey = $"preferred_language:{userId}";
        var userLang = await _storage.GetAsync(userKey);
        var globalLang = await _storage.GetAsync("preferred_language");
        var selected = userLang ?? globalLang;

        if (!string.IsNullOrWhiteSpace(selected))
        {
            await SetCultureAsync(selected!, userId, persist: false);
        }
        else
        {
            // Ensure default applied
            await SetCultureAsync(DefaultCulture.Name, userId, persist: false);
        }
    }

    private static void ApplyCulture(CultureInfo culture)
    {
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}
