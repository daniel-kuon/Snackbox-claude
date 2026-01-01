using System.Globalization;
using Snackbox.Components.Services;

namespace Snackbox.Components.Tests.Services;

public class LocalizationServiceTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultCultureToEnglish()
    {
        // Arrange & Act
        var service = new LocalizationService();

        // Assert
        Assert.Equal("en", service.CurrentCulture.TwoLetterISOLanguageName);
    }

    [Fact]
    public void SetCulture_ShouldUpdateCurrentCulture()
    {
        // Arrange
        var service = new LocalizationService();

        // Act
        service.SetCulture("de");

        // Assert
        Assert.Equal("de", service.CurrentCulture.TwoLetterISOLanguageName);
    }

    [Fact]
    public void SetCulture_ShouldUpdateThreadCultures()
    {
        // Arrange
        var service = new LocalizationService();

        // Act
        service.SetCulture("de");

        // Assert
        Assert.Equal("de", CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
        Assert.Equal("de", CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
    }

    [Fact]
    public void SetCulture_ShouldRaiseOnCultureChangedEvent()
    {
        // Arrange
        var service = new LocalizationService();
        var eventRaised = false;
        service.OnCultureChanged += () => eventRaised = true;

        // Act
        service.SetCulture("de");

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void SetCulture_ShouldNotRaiseEventIfCultureIsUnchanged()
    {
        // Arrange
        var service = new LocalizationService();
        service.SetCulture("de");
        var eventRaiseCount = 0;
        service.OnCultureChanged += () => eventRaiseCount++;

        // Act
        service.SetCulture("de"); // Same culture

        // Assert
        Assert.Equal(0, eventRaiseCount);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("fr")]
    [InlineData("es")]
    public void SetCulture_ShouldAcceptValidCultureCodes(string cultureCode)
    {
        // Arrange
        var service = new LocalizationService();

        // Act & Assert
        var exception = Record.Exception(() => service.SetCulture(cultureCode));
        Assert.Null(exception);
    }
}
