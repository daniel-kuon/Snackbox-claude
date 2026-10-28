namespace Snackbox.Api.Dtos;

/// <summary>When a theme dresses up the kiosk.</summary>
public enum ThemeMode
{
    /// <summary>Never.</summary>
    Off = 0,
    /// <summary>Every year between the start and end day (both included; may wrap over New Year).</summary>
    Dates = 1,
    /// <summary>Always, until switched back.</summary>
    Always = 2,
    /// <summary>On the theme's own day, worked out for every year (Black Friday, St. Nikolaus) - see ThemeCalendar.</summary>
    Auto = 3
}

public class ThemeDto
{
    public required string Key { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public ThemeMode Mode { get; set; }
    public int StartMonth { get; set; }
    public int StartDay { get; set; }
    public int EndMonth { get; set; }
    public int EndDay { get; set; }

    /// <summary>Whether the theme is on right now (when themes overlap, the kiosk shows the shortest).</summary>
    public bool Active { get; set; }

    /// <summary>For a theme with its own day: the next one (today included), otherwise null.</summary>
    public DateTime? AutoDay { get; set; }
}

public class UpdateThemeDto
{
    public ThemeMode Mode { get; set; }
    public int StartMonth { get; set; }
    public int StartDay { get; set; }
    public int EndMonth { get; set; }
    public int EndDay { get; set; }
}

/// <summary>The theme the kiosk shows now, or null for the normal look.</summary>
public class ActiveThemeDto
{
    public string? Key { get; set; }
}
