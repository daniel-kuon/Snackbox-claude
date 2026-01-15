using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.UI.Windowing;
using Snackbox.Web.Configuration;
using WinRT.Interop;

namespace Snackbox.Web.Services;

public class WindowsWindowService : IWindowService, IDisposable
{
    private Window? _window;
    private IntPtr _windowHandle;
    private AppWindow? _appWindow;
    private readonly bool _startFullscreen;
    private bool _isCurrentlyFullscreen;
    private WndProcDelegate? _wndProcDelegate;
    private IntPtr _oldWndProc;
    private bool _isMaximized = false;

    public WindowsWindowService(IConfiguration configuration)
    {
        var windowConfig = configuration.GetSection("Window").Get<WindowConfiguration>() ?? new WindowConfiguration();
        _startFullscreen = windowConfig.StartFullscreen;
    }

    public void SetWindow(Window window)
    {
        _window = window;

        // Get window handle and AppWindow
        var platformWindow = window.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        if (platformWindow != null)
        {
            _windowHandle = WindowNative.GetWindowHandle(platformWindow);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_windowHandle);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            if (_appWindow != null && _startFullscreen)
            {
                SetFullscreen(true);

                // Subclass window to intercept messages
                _wndProcDelegate = WndProc;
                _oldWndProc = SetWindowLongPtr(_windowHandle, GWLP_WNDPROC,
                    Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));
            }
        }
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case WM_SIZE:
                {
                    int sizeType = (int)wParam;

                    if (sizeType == SIZE_MAXIMIZED)
                    {
                        // Window was maximized - enter fullscreen if configured
                        if (_startFullscreen && !_isCurrentlyFullscreen)
                        {
                            _isMaximized = true;
                            _window?.Dispatcher.Dispatch(() => SetFullscreen(true));
                        }
                    }
                    else if (sizeType == SIZE_RESTORED && _isMaximized)
                    {
                        // Window was restored from maximized - exit fullscreen
                        _isMaximized = false;
                        if (_isCurrentlyFullscreen)
                        {
                            _window?.Dispatcher.Dispatch(() => SetFullscreen(false));
                        }
                    }
                    break;
                }
            case WM_SYSCOMMAND:
                {
                    int command = (int)wParam & 0xFFF0;

                    if (command == SC_MAXIMIZE)
                    {
                        // User clicked maximize button - will trigger WM_SIZE with SIZE_MAXIMIZED
                        _isMaximized = true;
                    }
                    else if (command == SC_RESTORE)
                    {
                        // User clicked restore button - will trigger WM_SIZE with SIZE_RESTORED
                        _isMaximized = false;
                    }
                    break;
                }
            case WM_ACTIVATE:
                {
                    int loWord = (int)wParam & 0xFFFF;
                    bool isActivating = loWord != WA_INACTIVE;

                    if (!isActivating && _isCurrentlyFullscreen)
                    {
                        // Window is losing focus while in fullscreen - exit fullscreen but stay maximized
                        _window?.Dispatcher.Dispatch(() => SetFullscreen(false));
                    }
                    break;
                }
            case WM_KILLFOCUS:
                {
                    // Window lost keyboard focus while in fullscreen - exit fullscreen
                    if (_isCurrentlyFullscreen)
                    {
                        _window?.Dispatcher.Dispatch(() => SetFullscreen(false));
                    }
                    break;
                }
        }

        // Call original window procedure
        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    public void BringToFront()
    {
        if (_windowHandle == IntPtr.Zero) return;

        try
        {
            // If minimized, restore it
            if (IsIconic(_windowHandle))
            {
                ShowWindow(_windowHandle, SW_RESTORE);
            }

            // Try multiple methods to ensure window comes to front
            SetForegroundWindow(_windowHandle);

            // Force window to top of Z-order
            SetWindowPos(_windowHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            SetWindowPos(_windowHandle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

            // Activate the window
            BringWindowToTop(_windowHandle);
            SetActiveWindow(_windowHandle);

            // Flash to get attention
            FlashWindow(_windowHandle, true);

            // Check if window is maximized and restore fullscreen if needed
            if (IsZoomed(_windowHandle))
            {
                _isMaximized = true;
                if (_startFullscreen && !_isCurrentlyFullscreen)
                {
                    _window?.Dispatcher.Dispatch(() => SetFullscreen(true));
                }
            }
        }
        catch
        {
            // Silently fail if we can't bring window to foreground
        }
    }

    public void SetFullscreen(bool fullscreen)
    {
        if (_appWindow == null) return;

        try
        {
            if (fullscreen)
            {
                _appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                _isCurrentlyFullscreen = true;
            }
            else
            {
                _appWindow.SetPresenter(AppWindowPresenterKind.Default);
                _isCurrentlyFullscreen = false;
            }
        }
        catch
        {
            // Silently fail
        }
    }

    public void Dispose()
    {
        // Restore original window procedure
        if (_windowHandle != IntPtr.Zero && _oldWndProc != IntPtr.Zero)
        {
            SetWindowLongPtr(_windowHandle, GWLP_WNDPROC, _oldWndProc);
        }
    }

    #region Win32 API
    private const int SW_RESTORE = 9;
    private const int SW_MAXIMIZE = 3;
    private const int SWP_NOMOVE = 0x0002;
    private const int SWP_NOSIZE = 0x0001;
    private const int SWP_SHOWWINDOW = 0x0040;
    private const int GWLP_WNDPROC = -4;
    private const uint WM_SIZE = 0x0005;
    private const uint WM_ACTIVATE = 0x0006;
    private const uint WM_KILLFOCUS = 0x0008;
    private const uint WM_SYSCOMMAND = 0x0112;
    private const int WA_INACTIVE = 0;
    private const int SIZE_RESTORED = 0;
    private const int SIZE_MAXIMIZED = 2;
    private const int SC_MAXIMIZE = 0xF030;
    private const int SC_RESTORE = 0xF120;
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlashWindow(IntPtr hWnd, bool bInvert);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsZoomed(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetActiveWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    #endregion
}
