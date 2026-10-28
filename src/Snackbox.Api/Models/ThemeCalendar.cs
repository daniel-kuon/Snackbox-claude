using Snackbox.Api.Dtos;

namespace Snackbox.Api.Models;

/// <summary>The days of the themes whose date moves from year to year (ThemeMode.Auto).</summary>
public static class ThemeCalendar
{
    public static DateTime? Day(string key, int year) => key switch
    {
        // The day after US Thanksgiving, the fourth Thursday of November
        ThemeKeys.BlackFriday => NthWeekday(year, 11, DayOfWeek.Thursday, 4).AddDays(1),
        // 6 December - on a weekend nobody is in the office, so the Monday after
        ThemeKeys.Nikolaus => Workday(new DateTime(year, 12, 6)),
        _ => null
    };

    private static DateTime NthWeekday(int year, int month, DayOfWeek weekday, int n)
    {
        var first = new DateTime(year, month, 1);
        return first.AddDays(((int)weekday - (int)first.DayOfWeek + 7) % 7 + 7 * (n - 1));
    }

    private static DateTime Workday(DateTime day) => day.DayOfWeek switch
    {
        DayOfWeek.Saturday => day.AddDays(2),
        DayOfWeek.Sunday => day.AddDays(1),
        _ => day
    };
}
