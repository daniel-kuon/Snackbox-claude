using Snackbox.Api.Dtos;

namespace Snackbox.Api.Models;

/// <summary>
/// A look the kiosk can dress up in (Halloween, ...), with its own achievements. The decoration
/// itself is code in the kiosk (ThemeScene); this row decides when it shows.
/// </summary>
public class Theme
{
    public int Id { get; set; }
    public required string Key { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public ThemeMode Mode { get; set; }

    // Yearly window, both days included; the end may lie before the start (over New Year)
    public int StartMonth { get; set; }
    public int StartDay { get; set; }
    public int EndMonth { get; set; }
    public int EndDay { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsActiveOn(DateTime localDate) => Mode switch
    {
        ThemeMode.Always => true,
        ThemeMode.Dates => InWindow(localDate),
        ThemeMode.Auto => AutoDay(localDate.Year) == localDate.Date,
        _ => false
    };

    /// <summary>This theme's own day in a year (ThemeMode.Auto), or null if it has none.</summary>
    public DateTime? AutoDay(int year) => ThemeCalendar.Day(Key, year);

    /// <summary>The first day of the run the date falls in - for "during Halloween week" counts.</summary>
    public DateTime PeriodStart(DateTime localDate)
    {
        if (Mode == ThemeMode.Auto && AutoDay(localDate.Year) is { } day && day <= localDate.Date) return day;
        if (Mode != ThemeMode.Dates) return new DateTime(localDate.Year, 1, 1);
        var start = Day(localDate.Year, StartMonth, StartDay);
        return start <= localDate.Date ? start : Day(localDate.Year - 1, StartMonth, StartDay);
    }

    /// <summary>How many days a run lasts. Of two themes on at once the kiosk shows the shorter: St. Nikolaus within Christmas.</summary>
    public int RunDays => Mode switch
    {
        ThemeMode.Auto => 1,
        ThemeMode.Dates => DatesRunDays(),
        _ => 366
    };

    private int DatesRunDays()
    {
        var days = (Day(2001, EndMonth, EndDay) - Day(2001, StartMonth, StartDay)).Days;
        return days >= 0 ? days + 1 : days + 366; // over New Year
    }

    private bool InWindow(DateTime date)
    {
        var day = date.Month * 100 + date.Day;
        var start = StartMonth * 100 + StartDay;
        var end = EndMonth * 100 + EndDay;
        return start <= end ? day >= start && day <= end : day >= start || day <= end;
    }

    // 29 February in a year without one becomes the 28th
    private static DateTime Day(int year, int month, int day) => new(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));
}
