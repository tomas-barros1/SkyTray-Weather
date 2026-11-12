using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace WinuiWeatherForecastTray
{
    /// <summary>
    /// Shared setup for the borderless, always-on-top popup windows (weather panel and settings).
    /// Sizes are given in device-independent pixels (DIPs) and scaled to the window's DPI, because
    /// <see cref="AppWindow.Resize"/> works in physical pixels while XAML layouts work in DIPs.
    /// </summary>
    internal static class PopupWindowHelper
    {
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        /// <summary>Removes the title bar/border, keeps the window on top and applies backdrop + rounded corners.</summary>
        public static void ApplyChrome(Window window, IntPtr hwnd, AppWindow appWindow)
        {
            window.ExtendsContentIntoTitleBar = true;

            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsResizable = false;
                presenter.IsMinimizable = false;
                presenter.IsMaximizable = false;
                presenter.IsAlwaysOnTop = true;
                presenter.SetBorderAndTitleBar(false, false);
            }

            if (DesktopAcrylicController.IsSupported())
            {
                window.SystemBackdrop = new DesktopAcrylicBackdrop();
            }
            else if (MicaController.IsSupported())
            {
                window.SystemBackdrop = new MicaBackdrop();
            }

            int cornerPref = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(int));
        }

        /// <summary>Display scale of the window's current monitor (1.0 = 100%, 1.5 = 150%...).</summary>
        public static double GetScale(IntPtr hwnd)
        {
            uint dpi = GetDpiForWindow(hwnd);
            return dpi == 0 ? 1.0 : dpi / 96.0;
        }

        /// <summary>Resizes the window to the given size in DIPs, converted to physical pixels.</summary>
        public static void ResizeToDips(IntPtr hwnd, AppWindow appWindow, int widthDips, int heightDips)
        {
            double scale = GetScale(hwnd);
            appWindow.Resize(new SizeInt32(
                (int)Math.Round(widthDips * scale),
                (int)Math.Round(heightDips * scale)));
        }
    }
}
