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

    public LocalizationService()
    {
        // Default to English
        _currentCulture = new CultureInfo("en");
    }

    public CultureInfo CurrentCulture => _currentCulture;

    public event Action? OnCultureChanged;

    public void SetCulture(string culture)
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
}
