using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Snackbox.Components.Services;
using Timer = System.Timers.Timer;

namespace Snackbox.Web.Services;

public class WindowsScannerListener : IDisposable, IScannerListener
{
    private const int WhKeyboardLl = 13;
    private const int WmKeydown = 0x0100;
    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookId = IntPtr.Zero;
    private readonly StringBuilder _buffer = new();
    private readonly Timer _resetTimer;
    private string? _lastCode;
    private DateTime? _lastCodeTime;

    public event Action<string>? CodeReceived;

    public WindowsScannerListener()
    {
        _proc = HookCallback;
        _resetTimer = new Timer(200);
        _resetTimer.Elapsed += (_, _) => ResetBuffer();
        _resetTimer.AutoReset = false;
    }

    public void Start() => _hookId = SetHook(_proc);

    public void Stop() => UnhookWindowsHookEx(_hookId);

    private void ResetBuffer()
    {
        lock (_buffer)
        {
            _buffer.Clear();
        }
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        return SetWindowsHookEx(WhKeyboardLl, proc, GetModuleHandle(curModule!.ModuleName), 0);
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == WmKeydown)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            var key = (VirtualKeys)vkCode;

            // Restart timer on any key press
            _resetTimer.Stop();
            _resetTimer.Start();

            if (key == VirtualKeys.Return)
            {
                string code;
                lock (_buffer)
                {
                    code = _buffer.ToString();

                    _buffer.Clear();
                }

                if (!string.IsNullOrEmpty(code))
                {
                    _resetTimer.Stop();

                    // Bring window to foreground
                    BringWindowToForeground();

                    // Invoke on UI thread if needed, or handle in component
                    // Ignore duplicate code within 500ms to prevent accidental scanning
                    if (code != _lastCode || _lastCodeTime == null ||
                                                (DateTime.Now - _lastCodeTime.Value).TotalMilliseconds > 500)
                    {
                        _lastCode = code;
                        _lastCodeTime = DateTime.Now;
                        CodeReceived?.Invoke(code);
                    }
                }
            }
            else if (IsDigit(key))
            {
                lock (_buffer)
                {
                    _buffer.Append(GetDigit(key));
                }
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private bool IsDigit(VirtualKeys key) =>
        (key >= VirtualKeys.Number0 && key <= VirtualKeys.Number9) ||
        (key >= VirtualKeys.Numpad0 && key <= VirtualKeys.Numpad9);

    private char GetDigit(VirtualKeys key)
    {
        if (key >= VirtualKeys.Numpad0) return (char)('0' + (key - VirtualKeys.Numpad0));
        return (char)('0' + (key - VirtualKeys.Number0));
    }

    private void BringWindowToForeground()
    {
        try
        {
            var handle = GetForegroundWindow();
            if (handle == IntPtr.Zero)
            {
                // Get the main window handle of current process
                var process = Process.GetCurrentProcess();
                handle = process.MainWindowHandle;
            }

            if (handle != IntPtr.Zero)
            {
                // Restore window if minimized
                ShowWindow(handle, SwRestore);
                // Bring to foreground
                SetForegroundWindow(handle);
                // Flash window to get attention
                FlashWindow(handle, true);
            }
        }
        catch
        {
            // Silently fail if we can't bring window to foreground
        }
    }

    public void Dispose()
    {
        Stop();
        _resetTimer.Dispose();
    }

    #region Win32 API
    private const int SwRestore = 9;

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlashWindow(IntPtr hWnd, bool bInvert);

    private enum VirtualKeys
    {
        Return = 0x0D,
        Number0 = 0x30, Number1, Number2, Number3, Number4, Number5, Number6, Number7, Number8, Number9 = 0x39,
        Numpad0 = 0x60, Numpad1, Numpad2, Numpad3, Numpad4, Numpad5, Numpad6, Numpad7, Numpad8, Numpad9 = 0x69
    }
    #endregion
}
