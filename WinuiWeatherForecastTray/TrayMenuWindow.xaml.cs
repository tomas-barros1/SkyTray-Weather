using System;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.Graphics;
using Windows.System;
using WinRT.Interop;
using WinuiWeatherForecastTray.Native;
using WinuiWeatherForecastTray.Services;
using WinuiWeatherForecastTray.Services.Interfaces;

namespace WinuiWeatherForecastTray
{
    /// <summary>
    /// The tray icon's right-click menu (Settings / Exit) as a WinUI popup that matches the rest of the app.
    /// Unlike the hover card it takes focus on purpose: losing focus (clicking anywhere else, Alt+Tab) or pressing Esc
    /// closes it, which is how a popup menu is expected to behave.
    /// </summary>
    public sealed partial class TrayMenuWindow : Window
    {
        /// <summary>Menu size in device-independent pixels; converted to physical pixels per monitor DPI.</summary>
        public const int WidthDips = 196;
        public const int HeightDips = 92;

        private const int GapDips = 8;
        private const int MarginDips = 8;
        private const int SW_HIDE = 0;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private static readonly IntPtr HWND_TOPMOST = new(-1);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private readonly IntPtr _hwnd;

        public event EventHandler? SettingsRequested;
        public event EventHandler? ExitRequested;

        public TrayMenuWindow(II18nService i18nService)
        {
            InitializeComponent();

            _hwnd = WindowNative.GetWindowHandle(this);
            AppWindow appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(_hwnd));

            PopupWindowHelper.ApplyChrome(this, _hwnd, appWindow);
            appWindow.IsShownInSwitchers = false;

            TxtSettings.Text = i18nService.GetString("Settings", "Settings");
            TxtExit.Text = i18nService.GetString("Exit", "Exit");

            Activated += (s, e) =>
            {
                if (e.WindowActivationState == WindowActivationState.Deactivated)
                {
                    Hide();
                }
            };
        }

        public bool IsShown { get; private set; }

        /// <summary>Opens the menu next to <paramref name="anchor"/> (the tray icon, physical pixels) and gives it focus.</summary>
        public void ShowNear(RECT anchor)
        {
            int centerX = (anchor.Left + anchor.Right) / 2;
            int centerY = (anchor.Top + anchor.Bottom) / 2;
            RectInt32 work = DisplayArea.GetFromPoint(new PointInt32(centerX, centerY), DisplayAreaFallback.Nearest).WorkArea;

            RectInt32 bounds = Place(anchor, work);
            SetWindowPos(_hwnd, HWND_TOPMOST, bounds.X, bounds.Y, bounds.Width, bounds.Height, SWP_SHOWWINDOW);

            // The window may have landed on a monitor with a different DPI: place it again once its real scale is known.
            RectInt32 corrected = Place(anchor, work);
            if (corrected.Width != bounds.Width || corrected.Height != bounds.Height)
            {
                SetWindowPos(_hwnd, HWND_TOPMOST, corrected.X, corrected.Y, corrected.Width, corrected.Height, SWP_SHOWWINDOW);
            }

            IsShown = true;
            Activate();
            SetForegroundWindow(_hwnd); // needed so that clicking elsewhere deactivates (and therefore closes) the menu
            BtnSettings.Focus(FocusState.Programmatic);
        }

        public void Hide()
        {
            if (!IsShown) return;

            ShowWindow(_hwnd, SW_HIDE);
            IsShown = false;
        }

        private RectInt32 Place(RECT anchor, RectInt32 work)
        {
            double scale = PopupWindowHelper.GetScale(_hwnd);
            int width = (int)Math.Round(WidthDips * scale);
            int height = (int)Math.Round(HeightDips * scale);

            (int x, int y) = PopupPlacement.Calculate(
                anchor.Left, anchor.Top, anchor.Right, anchor.Bottom,
                work.X, work.Y, work.Width, work.Height,
                width, height,
                gap: (int)Math.Round(GapDips * scale), margin: (int)Math.Round(MarginDips * scale));

            return new RectInt32(x, y, width, height);
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            SettingsRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }

        private void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                Hide();
                e.Handled = true;
            }
        }
    }
}
