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
        _ => false
    };

    /// <summary>The first day of the run the date falls in - for "during Halloween week" counts.</summary>
    public DateTime PeriodStart(DateTime localDate)
    {
        if (Mode != ThemeMode.Dates) return new DateTime(localDate.Year, 1, 1);
        var start = Day(localDate.Year, StartMonth, StartDay);
        return start <= localDate.Date ? start : Day(localDate.Year - 1, StartMonth, StartDay);
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
