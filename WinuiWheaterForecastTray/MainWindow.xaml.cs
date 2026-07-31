using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WinuiWheaterForecastTray.Models;
using WinuiWheaterForecastTray.Services;
using WinuiWheaterForecastTray.Services.Interfaces;
using WinuiWheaterForecastTray.Tray;
using WinRT.Interop;

namespace WinuiWheaterForecastTray
{
    public sealed partial class MainWindow : Window
    {
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private const int GWLP_WNDPROC = -4;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private readonly IWeatherService _weatherService;
        private readonly II18nService _i18nService;
        private readonly ISettingsService _settingsService;
        private readonly TrayIconManager _trayIconManager;
        private readonly DispatcherTimer _autoRefreshTimer;
        private IntPtr _hwnd;
        private AppWindow _appWindow;
        private WndProcDelegate? _wndProcDelegate;
        private IntPtr _oldWndProc = IntPtr.Zero;
        private WeatherForecastData? _currentForecast;
        private SettingsWindow? _settingsWindow;
        private bool _isExiting = false;

        public MainWindow()
        {
            InitializeComponent();

            _hwnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            ConfigureWindow();
            SetupSubclassing();

            _i18nService = new I18nService();
            _settingsService = new SettingsService();
            IApiService apiService = new ApiService();
            ILocationService locationService = new LocationService();
            IGeocodingService geocodingService = new GeocodingService();
            ILocationService ipLocationService = new IpLocationService();
            IAirQualityService airQualityService = new AirQualityService();
            _weatherService = new WeatherService(apiService, locationService, geocodingService, ipLocationService, _i18nService, airQualityService);

            ApplyLocalizedStrings();

            _trayIconManager = new TrayIconManager(_i18nService);
            _trayIconManager.Initialize(_hwnd, "☀️", _i18nService.GetString("FetchingData", "Weather Forecast — Fetching..."));
            _trayIconManager.TrayIconClicked += TrayIconManager_TrayIconClicked;
            _trayIconManager.TrayIconHovered += TrayIconManager_TrayIconHovered;
            _trayIconManager.SettingsRequested += TrayIconManager_SettingsRequested;
            _trayIconManager.ExitRequested += TrayIconManager_ExitRequested;

            _autoRefreshTimer = new DispatcherTimer();
            _autoRefreshTimer.Tick += (s, e) => _ = RefreshWeatherAsync();
            UpdateRefreshTimerInterval();
            _autoRefreshTimer.Start();

            this.Activated += MainWindow_Activated;
            this.Closed += MainWindow_Closed;

            _ = RefreshWeatherAsync();
        }

        private void UpdateRefreshTimerInterval()
        {
            int minutes = _settingsService.GetRefreshIntervalMinutes();
            _autoRefreshTimer.Interval = TimeSpan.FromMinutes(minutes);
        }

        private void ApplyLocalizedStrings()
        {
            LblLoading.Text = _i18nService.GetString("FetchingData", "Fetching weather data...");
            LblHumidity.Text = $"💧 {_i18nService.GetString("Humidity", "Humidity")}";
            LblWind.Text = $"🌬️ {_i18nService.GetString("Wind", "Wind")}";
            LblCloudCover.Text = $"☁️ {_i18nService.GetString("CloudCover", "Clouds")}";
            LblRain.Text = $"☔ {_i18nService.GetString("Rain", "Rain")}";
            LblPressure.Text = $"⏲️ {_i18nService.GetString("Pressure", "Pressure")}";
            LblAirQuality.Text = $"🍃 {_i18nService.GetString("AirQuality", "Air Quality")}";
            LblUV.Text = $"☀️ {_i18nService.GetString("UV", "UV Index")}";
            LblSunrise.Text = $"🌅 {_i18nService.GetString("Sunrise", "Sunrise")}";
            LblSunset.Text = $"🌇 {_i18nService.GetString("Sunset", "Sunset")}";
            LblNext6Hours.Text = _i18nService.GetString("Next6Hours", "Next 6 Hours");
        }

        private void ConfigureWindow()
        {
            ExtendsContentIntoTitleBar = true;

            if (_appWindow != null)
            {
                _appWindow.IsShownInSwitchers = false;
                _appWindow.Resize(new SizeInt32(360, 510));

                var presenter = _appWindow.Presenter as OverlappedPresenter;
                if (presenter != null)
                {
                    presenter.IsResizable = false;
                    presenter.IsMinimizable = false;
                    presenter.IsMaximizable = false;
                    presenter.IsAlwaysOnTop = true;
                    presenter.SetBorderAndTitleBar(false, false);
                }

                _appWindow.Closing += (sender, args) =>
                {
                    if (!_isExiting)
                    {
                        args.Cancel = true;
                        HideWindowToTray();
                    }
                };
            }

            if (DesktopAcrylicController.IsSupported())
            {
                SystemBackdrop = new DesktopAcrylicBackdrop();
            }
            else if (MicaController.IsSupported())
            {
                SystemBackdrop = new MicaBackdrop();
            }

            int cornerPref = DWMWCP_ROUND;
            DwmSetWindowAttribute(_hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(int));

            PositionPopupNearTray();
        }

        private void PositionPopupNearTray()
        {
            if (_hwnd == IntPtr.Zero) return;

            WindowId windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
            if (displayArea != null)
            {
                int width = 360;
                int height = 510;
                int x = displayArea.WorkArea.X + displayArea.WorkArea.Width - width - 16;
                int y = displayArea.WorkArea.Y + displayArea.WorkArea.Height - height - 16;

                _appWindow.Move(new PointInt32(x, y));
            }
        }

        private void SetupSubclassing()
        {
            _wndProcDelegate = CustomWndProc;
            IntPtr newWndProcPtr = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);
            _oldWndProc = SetWindowLongPtr(_hwnd, GWLP_WNDPROC, newWndProcPtr);
        }

        private IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == TrayIconManager.WM_TRAYICON)
            {
                _trayIconManager.HandleTrayMessage(lParam.ToInt32());
                return IntPtr.Zero;
            }

            return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        private async Task RefreshWeatherAsync()
        {
            try
            {
                BtnRefresh.IsEnabled = false;
                LoadingStack.Visibility = Visibility.Visible;
                ContentGrid.Visibility = Visibility.Collapsed;

                _currentForecast = await _weatherService.GetForecastAsync().ConfigureAwait(true);

                if (_currentForecast != null)
                {
                    UpdateUI(_currentForecast);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Weather load error: {ex}");
            }
            finally
            {
                BtnRefresh.IsEnabled = true;
                LoadingStack.Visibility = Visibility.Collapsed;
                ContentGrid.Visibility = Visibility.Visible;
            }
        }

        private void UpdateUI(WeatherForecastData data)
        {
            TxtDate.Text = data.Current.DateString;
            TxtCity.Text = data.Current.CityName;
            TxtTemperature.Text = $"{Math.Round(data.Current.Temperature)}°C";
            TxtCondition.Text = data.Current.ConditionText;
            TxtFeelsLike.Text = $"{_i18nService.GetString("FeelsLike", "Feels like")} {Math.Round(data.Current.ApparentTemperature)}°C";
            TxtEmoji.Text = data.Current.Emoji;

            TxtHumidity.Text = $"{Math.Round(data.Current.Humidity)}%";
            TxtWind.Text = $"{Math.Round(data.Current.WindSpeed)} km/h";
            TxtCloudCover.Text = $"{Math.Round(data.Current.CloudCover)}%";
            TxtPrecipitation.Text = $"{data.Current.Precipitation:F1} mm/h";
            TxtPressure.Text = $"{data.Current.SurfacePressure:F1} hPa";
            TxtAirQuality.Text = data.Current.AirQualityText;
            TxtUV.Text = data.Current.UvIndexText;
            TxtSunrise.Text = data.Current.SunriseTime;
            TxtSunset.Text = data.Current.SunsetTime;

            HourlyItemsControl.ItemsSource = data.HourlyForecast;

            string trayEmoji = WeatherHelper.GetTrayEmoji(data.Current.WeatherCode, data.Current.IsDay);
            _trayIconManager.Update(trayEmoji, data.Current.SummaryText);
        }

        private void TrayIconManager_TrayIconClicked(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (IsWindowVisible(_hwnd))
                {
                    HideWindowToTray();
                }
                else
                {
                    ShowWindowToTray();
                }
            });
        }

        private void TrayIconManager_TrayIconHovered(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                PositionPopupNearTray();
            });
        }

        private void TrayIconManager_SettingsRequested(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (_settingsWindow == null)
                {
                    _settingsWindow = new SettingsWindow(_i18nService);
                    _settingsWindow.SettingsChanged += (s, args) => UpdateRefreshTimerInterval();
                    _settingsWindow.Closed += (s, args) => _settingsWindow = null;
                }
                _settingsWindow.Activate();
            });
        }

        private void TrayIconManager_ExitRequested(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                _isExiting = true;
                _autoRefreshTimer?.Stop();
                _trayIconManager?.Dispose();
                Application.Current.Exit();
                Environment.Exit(0);
            });
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                if (_settingsWindow == null)
                {
                    HideWindowToTray();
                }
            }
        }

        private void HideWindowToTray()
        {
            if (_hwnd != IntPtr.Zero)
            {
                ShowWindow(_hwnd, SW_HIDE);
            }
        }

        private void ShowWindowToTray()
        {
            if (_hwnd != IntPtr.Zero)
            {
                PositionPopupNearTray();
                ShowWindow(_hwnd, SW_SHOW);
                SetForegroundWindow(_hwnd);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            _ = RefreshWeatherAsync();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            HideWindowToTray();
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            _autoRefreshTimer?.Stop();
            _trayIconManager?.Dispose();
        }
    }
}
