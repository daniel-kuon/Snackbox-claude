using System.Globalization;

namespace Snackbox.Components.Services;

public interface ILocalizationService
{
    CultureInfo CurrentCulture { get; }
    IReadOnlyList<CultureInfo> SupportedCultures { get; }

    event Action<CultureInfo>? OnCultureChanged;

    Task SetCultureAsync(string culture, string? userId = null, bool persist = true);
    Task ApplyUserLanguageAsync(string userId);
}
