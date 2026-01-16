using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Configuration;
using Snackbox.Components.Services;
using Snackbox.Web.Configuration;
using Timer = System.Timers.Timer;

namespace Snackbox.Web.Services;

public partial class WindowsScannerListener : IDisposable, IScannerListener
{
    private const int WhKeyboardLl = 13;
    private const int WmKeydown = 0x0100;
    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookId = IntPtr.Zero;
    private readonly StringBuilder _buffer = new();
    private readonly Timer _resetTimer;
    private string? _lastCode;
    private DateTime? _lastCodeTime;
    private readonly bool _autoFocusOnScan;
    private readonly IWindowService _windowService;
    private DateTime _lastKeystrokeTime = DateTime.MinValue;
    private readonly List<DateTime> _keystrokeTimes = new();

    public event Action<string>? CodeReceived;

    public WindowsScannerListener(IConfiguration configuration, IWindowService windowService)
    {
        _proc = HookCallback;
        _resetTimer = new Timer(200);
        _resetTimer.Elapsed += (_, _) => ResetBuffer();
        _resetTimer.AutoReset = false;
        _windowService = windowService;

        var windowConfig = configuration.GetSection("Window").Get<WindowConfiguration>() ?? new WindowConfiguration();
        _autoFocusOnScan = windowConfig.AutoFocusOnScan;
    }

    public void Start()
    {
        // Only set hook if not already set (prevent multiple hooks)
        if (_hookId == IntPtr.Zero)
        {
            _hookId = SetHook(_proc);
        }
    }

    public void Stop()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

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

                    // Bring window to foreground if enabled
                    if (_autoFocusOnScan)
                    {
                        _windowService.BringToFront();
                    }

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

    public void Dispose()
    {
        Stop();
        _resetTimer.Dispose();
    }

    #region Win32 API
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private enum VirtualKeys
    {
        Return = 0x0D,
        // ReSharper disable UnusedMember.Local
        Number0 = 0x30, Number1, Number2, Number3, Number4, Number5, Number6, Number7, Number8, Number9 = 0x39,
        Numpad0 = 0x60, Numpad1, Numpad2, Numpad3, Numpad4, Numpad5, Numpad6, Numpad7, Numpad8, Numpad9 = 0x69
        // ReSharper restore UnusedMember.Local
    }
    #endregion
}
