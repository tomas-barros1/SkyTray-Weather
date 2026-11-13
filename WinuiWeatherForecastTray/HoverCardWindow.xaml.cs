using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinRT.Interop;
using WinuiWeatherForecastTray.Models;
using WinuiWeatherForecastTray.Native;
using WinuiWeatherForecastTray.Services;
using WinuiWeatherForecastTray.Services.Interfaces;

namespace WinuiWeatherForecastTray
{
    /// <summary>
    /// The card shown when hovering the tray icon: current conditions, today's high/low, humidity/wind/rain and the
    /// next hours. It is shown without ever taking focus (WS_EX_NOACTIVATE + SW_SHOWNOACTIVATE-style placement), so
    /// hovering the tray never steals the keyboard focus from the app the user is working in.
    /// </summary>
    public sealed partial class HoverCardWindow : Window
    {
        /// <summary>Card size in device-independent pixels; converted to physical pixels per monitor DPI.</summary>
        public const int WidthDips = 300;
        public const int HeightDips = 228;

        private const int GapDips = 8;
        private const int MarginDips = 8;
        private const int HourlySlots = 4;

        private const int GWL_EXSTYLE = -20;
        private const long WS_EX_NOACTIVATE = 0x08000000L;
        private const long WS_EX_TOOLWINDOW = 0x00000080L;
        private const int SW_HIDE = 0;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private static readonly IntPtr HWND_TOPMOST = new(-1);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private readonly IntPtr _hwnd;
        private readonly II18nService _i18nService;

        public HoverCardWindow(II18nService i18nService)
        {
            _i18nService = i18nService;
            InitializeComponent();

            _hwnd = WindowNative.GetWindowHandle(this);
            AppWindow appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(_hwnd));

            PopupWindowHelper.ApplyChrome(this, _hwnd, appWindow);
            appWindow.IsShownInSwitchers = false;

            // Tool window that never becomes the active window: hovering must not steal focus.
            long exStyle = GetWindowLongPtr(_hwnd, GWL_EXSTYLE).ToInt64();
            SetWindowLongPtr(_hwnd, GWL_EXSTYLE, new IntPtr(exStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW));

            LblHumidity.Text = _i18nService.GetString("Humidity", "Humidity");
            LblWind.Text = _i18nService.GetString("Wind", "Wind");
            LblRain.Text = _i18nService.GetString("Rain", "Rain");
        }

        /// <summary>Screen rectangle (physical pixels) of the card while it is shown.</summary>
        public RectInt32 Bounds { get; private set; }

        public bool IsShown { get; private set; }

        /// <summary>Fills the card with the latest forecast. Safe to call while hidden.</summary>
        public void Update(WeatherForecastData data)
        {
            CurrentWeatherInfo current = data.Current;

            string updated = WeatherText.UpdatedLabel(_i18nService, data);
            TxtUpdated.Text = data.IsStale ? $"⚠ {updated}" : updated;

            TxtCity.Text = current.CityName;
            TxtTemp.Text = $"{Math.Round(current.Temperature)}°";
            TxtCondition.Text = current.ConditionText;
            TxtFeels.Text = $"{_i18nService.GetString("FeelsLike", "Feels like")} {Math.Round(current.ApparentTemperature)}°";

            if (current.TemperatureMax is double max && current.TemperatureMin is double min)
            {
                TxtHighLow.Text = string.Format(
                    _i18nService.GetString("HighLow", "High {0}° · Low {1}°"), Math.Round(max), Math.Round(min));
                TxtHighLow.Visibility = Visibility.Visible;
            }
            else
            {
                TxtHighLow.Visibility = Visibility.Collapsed;
            }

            ImgIcon.Source = WeatherIconSource.Get(WeatherHelper.GetIcon(current.WeatherCode, current.IsDay));
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(ImgIcon, current.ConditionText);

            TxtHumidity.Text = $"{Math.Round(current.Humidity)}%";
            TxtWind.Text = $"{Math.Round(current.WindSpeed)} km/h";
            TxtRain.Text = $"{Math.Round(current.PrecipitationProbability)}%";

            HourlyItems.ItemsSource = data.HourlyForecast.Take(HourlySlots).ToList();
        }

        /// <summary>Shows the card next to <paramref name="anchor"/> (the tray icon, physical pixels) without activating it.</summary>
        public void ShowNear(RECT anchor)
        {
            int centerX = (anchor.Left + anchor.Right) / 2;
            int centerY = (anchor.Top + anchor.Bottom) / 2;
            RectInt32 work = DisplayArea.GetFromPoint(new PointInt32(centerX, centerY), DisplayAreaFallback.Nearest).WorkArea;

            RectInt32 bounds = Place(anchor, work);
            SetWindowPos(_hwnd, HWND_TOPMOST, bounds.X, bounds.Y, bounds.Width, bounds.Height, SWP_NOACTIVATE | SWP_SHOWWINDOW);

            // The window may have landed on a monitor with a different DPI than the one it was on before:
            // place it again once, now that its real scale is known.
            RectInt32 corrected = Place(anchor, work);
            if (corrected.Width != bounds.Width || corrected.Height != bounds.Height)
            {
                bounds = corrected;
                SetWindowPos(_hwnd, HWND_TOPMOST, bounds.X, bounds.Y, bounds.Width, bounds.Height, SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }

            Bounds = bounds;
            IsShown = true;
        }

        public void Hide()
        {
            ShowWindow(_hwnd, SW_HIDE);
            IsShown = false;
        }

        /// <summary>Centers the card on the icon, above it for a bottom taskbar (below for a top one), inside the work area.</summary>
        private RectInt32 Place(RECT anchor, RectInt32 work)
        {
            double scale = PopupWindowHelper.GetScale(_hwnd);
            int width = (int)Math.Round(WidthDips * scale);
            int height = (int)Math.Round(HeightDips * scale);
            int gap = (int)Math.Round(GapDips * scale);
            int margin = (int)Math.Round(MarginDips * scale);

            int minX = work.X + margin;
            int maxX = Math.Max(minX, work.X + work.Width - width - margin);
            int x = Math.Clamp((anchor.Left + anchor.Right) / 2 - width / 2, minX, maxX);

            bool above = (anchor.Top + anchor.Bottom) / 2 >= work.Y + work.Height / 2;
            int y = above ? anchor.Top - height - gap : anchor.Bottom + gap;

            int minY = work.Y + margin;
            int maxY = Math.Max(minY, work.Y + work.Height - height - margin);
            y = Math.Clamp(y, minY, maxY);

            return new RectInt32(x, y, width, height);
        }
    }
}
