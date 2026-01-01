using System.Globalization;

namespace Snackbox.Components.Services;

public interface ILocalizationService
{
    /// <summary>
    /// Gets the current culture.
    /// </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    /// Sets the culture and notifies subscribers.
    /// </summary>
    /// <param name="culture">The culture code (e.g., "en", "de")</param>
    void SetCulture(string culture);

    /// <summary>
    /// Event raised when culture changes.
    /// </summary>
    event Action? OnCultureChanged;
}

public class LocalizationService : ILocalizationService
{
    private CultureInfo _currentCulture;
    private static readonly string[] SupportedLanguages = { "en", "de" };

    public LocalizationService()
    {
        // Default to English
        _currentCulture = new CultureInfo("en");
    }

    public CultureInfo CurrentCulture => _currentCulture;

    public event Action? OnCultureChanged;

    public void SetCulture(string culture)
    {
        try
        {
            var newCulture = new CultureInfo(culture);
            
            if (!_currentCulture.Equals(newCulture))
            {
                _currentCulture = newCulture;
                CultureInfo.CurrentCulture = newCulture;
                CultureInfo.CurrentUICulture = newCulture;
                
                OnCultureChanged?.Invoke();
            }
        }
        catch (CultureNotFoundException)
        {
            // If invalid culture code provided, fall back to English
            var fallbackCulture = new CultureInfo("en");
            if (!_currentCulture.Equals(fallbackCulture))
            {
                _currentCulture = fallbackCulture;
                CultureInfo.CurrentCulture = fallbackCulture;
                CultureInfo.CurrentUICulture = fallbackCulture;
                
                OnCultureChanged?.Invoke();
            }
        }
    }
}
