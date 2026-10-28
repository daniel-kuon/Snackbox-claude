using System.Runtime.InteropServices;

namespace Snackbox.Updater;

/// <summary>
/// A click into a console window with QuickEdit on (Windows 10's default) starts a selection and
/// holds every write to that console until it ends - and with it whoever writes. On the kiosk PC
/// an update stood still for minutes that way, with the stack stopped. The update's window gets
/// it switched off.
/// </summary>
public static class QuickEdit
{
    private const int StdInputHandle = -10;
    private const uint EnableQuickEditMode = 0x0040;
    private const uint EnableExtendedFlags = 0x0080;

    public static void Disable()
    {
        var input = GetStdHandle(StdInputHandle);
        if (!GetConsoleMode(input, out var mode)) return; // no console, e.g. a scheduled task
        SetConsoleMode(input, (mode & ~EnableQuickEditMode) | EnableExtendedFlags);
    }

    [DllImport("kernel32.dll")]
    private static extern nint GetStdHandle(int handle);

    [DllImport("kernel32.dll")]
    private static extern bool GetConsoleMode(nint handle, out uint mode);

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(nint handle, uint mode);
}
