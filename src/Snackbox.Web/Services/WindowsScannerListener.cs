using System.Runtime.InteropServices;
using System.Text;
using Timer = System.Timers.Timer;

namespace Snackbox.Web.Services;

public class WindowsScannerListener : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookId = IntPtr.Zero;
    private readonly StringBuilder _buffer = new();
    private readonly Timer _resetTimer;

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
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
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
                    // Invoke on UI thread if needed, or handle in component
                    CodeReceived?.Invoke(code);
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
        Number0 = 0x30, Number1, Number2, Number3, Number4, Number5, Number6, Number7, Number8, Number9 = 0x39,
        Numpad0 = 0x60, Numpad1, Numpad2, Numpad3, Numpad4, Numpad5, Numpad6, Numpad7, Numpad8, Numpad9 = 0x69
    }
    #endregion
}
