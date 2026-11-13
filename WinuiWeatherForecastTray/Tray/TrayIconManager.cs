using System;
using System.Runtime.InteropServices;
using WinuiWeatherForecastTray.Models;
using WinuiWeatherForecastTray.Native;
using WinuiWeatherForecastTray.Services;
using WinuiWeatherForecastTray.Services.Interfaces;

namespace WinuiWeatherForecastTray.Tray;

public sealed class TrayIconManager : IDisposable
{
    private const uint WM_USER = 0x0400;
    public const uint WM_TRAYICON = WM_USER + 1;

    /// <summary>
    /// Broadcast by Explorer whenever the taskbar is (re)created, e.g. after an Explorer crash/restart.
    /// Tray icons are lost at that point and must be added again.
    /// </summary>
    public static readonly uint WM_TASKBARCREATED = RegisterWindowMessageW("TaskbarCreated");

    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_MODIFY = 0x00000001;
    private const uint NIM_DELETE = 0x00000002;

    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_TIP = 0x00000004;

    public const int WM_MOUSEMOVE = 0x0200;
    public const int WM_LBUTTONUP = 0x0202;
    public const int WM_RBUTTONUP = 0x0205;

    private const uint MF_STRING = 0x00000000;
    private const uint TPM_RETURNCMD = 0x0100;
    private const uint TPM_RIGHTBUTTON = 0x0002;

    private const ulong ID_SETTINGS = 101;
    private const ulong ID_EXIT = 102;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("shell32.dll")]
    private static extern int Shell_NotifyIconGetRect(ref NOTIFYICONIDENTIFIER identifier, out RECT iconLocation);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessageW(string lpString);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool AppendMenuW(IntPtr hMenu, uint uFlags, ulong uIDNewItem, string lpNewItem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint TrackPopupMenuEx(IntPtr hMenu, uint uFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private IntPtr _hwnd;
    private IntPtr _currentHIcon = IntPtr.Zero;
    private bool _isAdded = false;
    private string _lastTooltip = string.Empty;
    private bool _classicTooltipEnabled = true;
    private readonly II18nService _i18nService;

    public event EventHandler? TrayIconClicked;
    public event EventHandler? TrayIconHovered;
    public event EventHandler? SettingsRequested;
    public event EventHandler? ExitRequested;

    public TrayIconManager(II18nService? i18nService = null)
    {
        _i18nService = i18nService ?? new I18nService();
    }

    public void Initialize(IntPtr hwnd, WeatherIcon initialIcon = WeatherIcon.Loading, string initialTooltip = "Weather Forecast")
    {
        _hwnd = hwnd;
        _currentHIcon = TrayIconHelper.CreateIcon(initialIcon);
        _lastTooltip = initialTooltip;

        AddIcon();
    }

    /// <summary>
    /// Registers the icon with the shell using the current icon and tooltip. This can fail when the taskbar
    /// does not exist yet (autostart right after login); <see cref="Update"/> then retries on its next call.
    /// </summary>
    private void AddIcon()
    {
        var nid = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = 1001,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_TRAYICON,
            hIcon = _currentHIcon,
            szTip = CurrentTip()
        };

        _isAdded = Shell_NotifyIcon(NIM_ADD, ref nid);
    }

    /// <summary>The tooltip text to send to the shell: empty while the classic tooltip is turned off.</summary>
    private string CurrentTip() => _classicTooltipEnabled ? TruncateTooltip(_lastTooltip) : string.Empty;

    /// <summary>
    /// Turns the classic Windows tooltip on or off. It is turned off while the hover card is enabled,
    /// otherwise both would appear at the same time.
    /// </summary>
    public void SetClassicTooltipEnabled(bool enabled)
    {
        if (_classicTooltipEnabled == enabled) return;

        _classicTooltipEnabled = enabled;
        if (_hwnd == IntPtr.Zero || !_isAdded) return;

        var nid = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = 1001,
            uFlags = NIF_TIP,
            szTip = CurrentTip()
        };

        Shell_NotifyIcon(NIM_MODIFY, ref nid);
    }

    /// <summary>
    /// Gets the icon's rectangle on screen (physical pixels). Returns <c>false</c> when the shell cannot say
    /// where the icon is (e.g. it was not added yet); callers should then fall back to the cursor position.
    /// </summary>
    public bool TryGetIconRect(out RECT rect)
    {
        rect = default;
        if (_hwnd == IntPtr.Zero || !_isAdded) return false;

        var id = new NOTIFYICONIDENTIFIER
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONIDENTIFIER>(),
            hWnd = _hwnd,
            uID = 1001,
            guidItem = Guid.Empty
        };

        int hr = Shell_NotifyIconGetRect(ref id, out rect);
        return hr >= 0 && rect.Right > rect.Left && rect.Bottom > rect.Top;
    }

    /// <summary>Adds the icon again after the taskbar was recreated (see <see cref="WM_TASKBARCREATED"/>).</summary>
    public void RestoreIcon()
    {
        if (_hwnd == IntPtr.Zero) return;

        AddIcon();
    }

    public void Update(WeatherIcon icon, string tooltipText)
    {
        if (_hwnd == IntPtr.Zero) return;

        IntPtr newHIcon = TrayIconHelper.CreateIcon(icon);
        _lastTooltip = tooltipText;

        if (!_isAdded)
        {
            // The first NIM_ADD failed (taskbar not ready yet): try again with the new icon instead of
            // staying without a tray icon for the whole session.
            IntPtr previousHIcon = _currentHIcon;
            _currentHIcon = newHIcon;
            AddIcon();

            if (previousHIcon != IntPtr.Zero)
            {
                TrayIconHelper.DestroyIcon(previousHIcon);
            }
            return;
        }

        var nid = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = 1001,
            uFlags = NIF_ICON | NIF_TIP,
            hIcon = newHIcon,
            szTip = CurrentTip()
        };

        Shell_NotifyIcon(NIM_MODIFY, ref nid);

        if (_currentHIcon != IntPtr.Zero)
        {
            TrayIconHelper.DestroyIcon(_currentHIcon);
        }

        _currentHIcon = newHIcon;
    }

    public void HandleTrayMessage(int lParam)
    {
        if (lParam == WM_LBUTTONUP)
        {
            TrayIconClicked?.Invoke(this, EventArgs.Empty);
        }
        else if (lParam == WM_RBUTTONUP)
        {
            ShowContextMenu();
        }
        else if (lParam == WM_MOUSEMOVE)
        {
            TrayIconHovered?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ShowContextMenu()
    {
        if (_hwnd == IntPtr.Zero) return;

        IntPtr hMenu = CreatePopupMenu();
        if (hMenu == IntPtr.Zero) return;

        string settingsText = $"⚙️ {_i18nService.GetString("Settings", "Settings")}";
        string exitText = $"❌ {_i18nService.GetString("Exit", "Exit")}";

        AppendMenuW(hMenu, MF_STRING, ID_SETTINGS, settingsText);
        AppendMenuW(hMenu, MF_STRING, ID_EXIT, exitText);

        SetForegroundWindow(_hwnd);
        GetCursorPos(out POINT pt);

        uint cmd = TrackPopupMenuEx(hMenu, TPM_RETURNCMD | TPM_RIGHTBUTTON, pt.X, pt.Y, _hwnd, IntPtr.Zero);
        DestroyMenu(hMenu);

        if (cmd == ID_SETTINGS)
        {
            SettingsRequested?.Invoke(this, EventArgs.Empty);
        }
        else if (cmd == ID_EXIT)
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private static string TruncateTooltip(string tooltipText)
    {
        if (string.IsNullOrEmpty(tooltipText)) return string.Empty;
        return tooltipText.Length >= 127 ? tooltipText[..124] + "..." : tooltipText;
    }

    public void Dispose()
    {
        if (_isAdded && _hwnd != IntPtr.Zero)
        {
            var nid = new NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hwnd,
                uID = 1001
            };
            Shell_NotifyIcon(NIM_DELETE, ref nid);
            _isAdded = false;
        }

        if (_currentHIcon != IntPtr.Zero)
        {
            TrayIconHelper.DestroyIcon(_currentHIcon);
            _currentHIcon = IntPtr.Zero;
        }
    }
}
