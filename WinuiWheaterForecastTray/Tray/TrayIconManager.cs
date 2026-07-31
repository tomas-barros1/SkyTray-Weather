using System;
using System.Runtime.InteropServices;
using WinuiWheaterForecastTray.Native;
using WinuiWheaterForecastTray.Services;
using WinuiWheaterForecastTray.Services.Interfaces;

namespace WinuiWheaterForecastTray.Tray;

public sealed class TrayIconManager : IDisposable
{
    private const uint WM_USER = 0x0400;
    public const uint WM_TRAYICON = WM_USER + 1;

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
    private readonly II18nService _i18nService;

    public event EventHandler? TrayIconClicked;
    public event EventHandler? TrayIconHovered;
    public event EventHandler? SettingsRequested;
    public event EventHandler? ExitRequested;

    public TrayIconManager(II18nService? i18nService = null)
    {
        _i18nService = i18nService ?? new I18nService();
    }

    public void Initialize(IntPtr hwnd, string initialEmoji = "☀️", string initialTooltip = "Weather Forecast")
    {
        _hwnd = hwnd;
        _currentHIcon = TrayIconHelper.CreateEmojiIcon(initialEmoji);

        var nid = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = 1001,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_TRAYICON,
            hIcon = _currentHIcon,
            szTip = TruncateTooltip(initialTooltip)
        };

        _isAdded = Shell_NotifyIcon(NIM_ADD, ref nid);
    }

    public void Update(string emoji, string tooltipText)
    {
        if (!_isAdded || _hwnd == IntPtr.Zero) return;

        IntPtr newHIcon = TrayIconHelper.CreateEmojiIcon(emoji);

        var nid = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = 1001,
            uFlags = NIF_ICON | NIF_TIP,
            hIcon = newHIcon,
            szTip = TruncateTooltip(tooltipText)
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
