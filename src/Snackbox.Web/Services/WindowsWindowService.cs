using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Snackbox.Api.Dtos;
using Snackbox.ApiClient;
using Snackbox.Web.Configuration;
using WinRT.Interop;

namespace Snackbox.Web.Services;

public class WindowsWindowService : IWindowService, IDisposable
{
    private Window? _window;
    private IntPtr _windowHandle;
    private AppWindow? _appWindow;
    private readonly bool _fullscreenConfigured;
    private readonly IServiceProvider _services;
    private System.Threading.Timer? _modeTimer;
    private bool _isCurrentlyFullscreen;
    private bool _initialMinimizeDone;
    private WndProcDelegate? _wndProcDelegate;
    private IntPtr _oldWndProc;
    private bool _isMaximized = false;

    /// <summary>
    /// Parallel-run mode: minimized, never pulled to the front. Starts from Window:StartMinimized
    /// and then follows Admin -> Settings -> Kiosk window (polled), which wins once the API answers.
    /// </summary>
    public bool IsBackground { get; private set; }

    // Background owns the window state: never fullscreen while in the background
    private bool FullscreenWanted => _fullscreenConfigured && !IsBackground;

    public WindowsWindowService(IConfiguration configuration, IServiceProvider services)
    {
        var windowConfig = configuration.GetSection("Window").Get<WindowConfiguration>() ?? new WindowConfiguration();
        IsBackground = windowConfig.StartMinimized;
        _fullscreenConfigured = windowConfig.StartFullscreen;
        _services = services;
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

            if (_appWindow != null)
            {
                if (IsBackground)
                {
                    ShowWindow(_windowHandle, SW_MINIMIZE);
                }
                else
                {
                    SetFullscreen(true);
                }

                // Subclass window to intercept messages
                _wndProcDelegate = WndProc;
                _oldWndProc = SetWindowLongPtr(_windowHandle, GWLP_WNDPROC,
                    Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));

                _modeTimer = new System.Threading.Timer(_ => _ = PollModeAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
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
                        if (FullscreenWanted && !_isCurrentlyFullscreen)
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

                    // MAUI activates the window right after it is created, which undoes the
                    // minimize from SetWindow. Push it back down once - after that the user
                    // (or an admin) is free to open it.
                    if (isActivating && IsBackground && !_initialMinimizeDone)
                    {
                        _initialMinimizeDone = true;
                        _window?.Dispatcher.Dispatch(() => ShowWindow(_windowHandle, SW_MINIMIZE));
                    }

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

            // Windows refuses SetForegroundWindow from a process that does not own the
            // foreground (the old Snackbox does while running in parallel). Borrowing the
            // foreground thread's input queue lifts that block - without it the kiosk resizes
            // to fullscreen but stays behind the other window.
            var foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
            var currentThread = GetCurrentThreadId();
            var attached = foregroundThread != 0 && foregroundThread != currentThread &&
                           AttachThreadInput(currentThread, foregroundThread, true);

            try
            {
                SetForegroundWindow(_windowHandle);

                // Force window to top of Z-order
                SetWindowPos(_windowHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                SetWindowPos(_windowHandle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

                // Activate the window
                BringWindowToTop(_windowHandle);
                SetActiveWindow(_windowHandle);

                // Flash to get attention
                FlashWindow(_windowHandle, true);
            }
            finally
            {
                if (attached)
                {
                    AttachThreadInput(currentThread, foregroundThread, false);
                }
            }

            // Go back to fullscreen. Losing focus drops the presenter back to Default (see
            // WM_ACTIVATE), so the window is usually not "zoomed" at this point - checking for
            // that would leave it windowed after every scan.
            if (FullscreenWanted && !_isCurrentlyFullscreen)
            {
                _isMaximized = true;
                _window?.Dispatcher.Dispatch(() => SetFullscreen(true));
            }
        }
        catch
        {
            // Silently fail if we can't bring window to foreground
        }
    }

    private async Task PollModeAsync()
    {
        try
        {
            using var scope = _services.CreateScope();
            var flags = await scope.ServiceProvider.GetRequiredService<IFeatureFlagsApi>().GetAllAsync();
            var flag = flags.FirstOrDefault(f => f.Key == FeatureFlagKeys.KioskBackground);
            if (flag == null) return;

            var background = flag.Audience != FeatureAudience.Disabled;
            if (background == IsBackground) return;

            IsBackground = background;
            _window?.Dispatcher.Dispatch(() =>
            {
                if (background)
                {
                    // Out of the way for the old Snackbox
                    SetFullscreen(false);
                    ShowWindow(_windowHandle, SW_MINIMIZE);
                }
                else
                {
                    BringToFront();
                }
            });
        }
        catch
        {
            // API not reachable (yet) - keep the current mode and try again on the next tick
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
        _modeTimer?.Dispose();

        // Restore original window procedure
        if (_windowHandle != IntPtr.Zero && _oldWndProc != IntPtr.Zero)
        {
            SetWindowLongPtr(_windowHandle, GWLP_WNDPROC, _oldWndProc);
        }
    }

    #region Win32 API
    private const int SW_RESTORE = 9;
    private const int SW_MINIMIZE = 6;
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

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    #endregion
}
