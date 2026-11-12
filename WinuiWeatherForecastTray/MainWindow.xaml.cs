using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WinuiWeatherForecastTray.Models;
using WinuiWeatherForecastTray.Native;
using WinuiWeatherForecastTray.Services;
using WinuiWeatherForecastTray.Services.Interfaces;
using WinuiWeatherForecastTray.Tray;
using WinRT.Interop;

namespace WinuiWeatherForecastTray
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
        private bool _hasLoadedOnce = false;
        private int _startupRetryCount = 0;
        private bool _isRefreshing = false;
        private static readonly int[] StartupRetryDelaysSeconds = { 10, 30, 60 };
        private DateTime _lastDeactivatedTime = DateTime.MinValue;

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
            // Decorator: persists each successful forecast and serves the last one (flagged stale)
            // when a fetch fails, so the app still shows something useful while offline.
            _weatherService = new CachedWeatherService(
                new WeatherService(apiService, locationService, geocodingService, ipLocationService, _i18nService, airQualityService),
                new ForecastCacheService());

            // Keeps the "start with Windows" entry pointing at this executable after upgrades/renames.
            try { new AutostartService().RefreshAutostartPath(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Autostart refresh failed: {ex.Message}"); }

            ApplyLocalizedStrings();

            _trayIconManager = new TrayIconManager(_i18nService);
            _trayIconManager.Initialize(_hwnd, "🔄", _i18nService.GetString("FetchingData", "Weather Forecast — Fetching..."));
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

            _ = RefreshWeatherAsync(isStartup: true);
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

            ToolTipService.SetToolTip(BtnRefresh, _i18nService.GetString("RefreshTooltip", "Refresh weather"));
            ToolTipService.SetToolTip(BtnClose, _i18nService.GetString("CloseTooltip", "Close to tray"));
        }

        private void ConfigureWindow()
        {
            ExtendsContentIntoTitleBar = true;

            if (_appWindow != null)
            {
                _appWindow.IsShownInSwitchers = false;
                _appWindow.Resize(new SizeInt32(360, 524));

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
                int height = 524;
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

        private async Task RefreshWeatherAsync(bool isStartup = false)
        {
            // Timer ticks, manual refresh and startup retries can overlap; run one at a time.
            if (_isRefreshing)
                return;
            _isRefreshing = true;

            bool hadData = _currentForecast != null;
            try
            {
                BtnRefresh.IsEnabled = false;

                // Only blank the window on the very first load; later refreshes keep the
                // current data on screen instead of flashing a spinner every few minutes.
                if (!hadData)
                {
                    ShowLoading();
                    _trayIconManager?.Update("🔄", _i18nService.GetString("FetchingData", "Fetching weather data..."));
                }

                var forecast = await _weatherService.GetForecastAsync().ConfigureAwait(true);

                _currentForecast = forecast;
                ShowContent();
                UpdateUI(forecast);

                if (!forecast.IsStale)
                {
                    _hasLoadedOnce = true;
                    _startupRetryCount = 0;
                }
                else
                {
                    // Showing a cached copy: keep trying to get fresh data on startup.
                    ScheduleStartupRetryIfNeeded(isStartup);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Weather load error: {ex}");

                if (_currentForecast != null)
                {
                    // Keep showing what we already have, but mark it as out of date.
                    _currentForecast.IsStale = true;
                    ShowContent();
                    UpdateUI(_currentForecast);
                }
                else
                {
                    ShowLoadFailure();
                }

                ScheduleStartupRetryIfNeeded(isStartup);
            }
            finally
            {
                BtnRefresh.IsEnabled = true;
                _isRefreshing = false;
            }
        }

        /// <summary>On startup the network may not be ready yet: retry with backoff until data loads.</summary>
        private void ScheduleStartupRetryIfNeeded(bool isStartup)
        {
            if (isStartup && !_hasLoadedOnce && _startupRetryCount < StartupRetryDelaysSeconds.Length)
            {
                int delaySeconds = StartupRetryDelaysSeconds[_startupRetryCount++];
                System.Diagnostics.Debug.WriteLine($"[Startup] Network not ready. Retrying in {delaySeconds}s (attempt {_startupRetryCount})...");
                ScheduleStartupRetry(delaySeconds);
            }
        }

        private void ShowLoading()
        {
            LoadingRing.IsActive = true;
            LoadingRing.Visibility = Visibility.Visible;
            LblLoading.Text = _i18nService.GetString("FetchingData", "Fetching weather data...");
            LoadingStack.Visibility = Visibility.Visible;
            ContentGrid.Visibility = Visibility.Collapsed;
        }

        private void ShowContent()
        {
            LoadingRing.IsActive = false;
            LoadingStack.Visibility = Visibility.Collapsed;
            ContentGrid.Visibility = Visibility.Visible;
        }

        /// <summary>First load failed and there is nothing cached: say so instead of showing an empty window.</summary>
        private void ShowLoadFailure()
        {
            string message = _i18nService.GetString("FetchFailed", "Couldn't load weather data");
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
            LblLoading.Text = message;
            LoadingStack.Visibility = Visibility.Visible;
            ContentGrid.Visibility = Visibility.Collapsed;
            _trayIconManager?.Update("⚠️", message);
        }

        /// <summary>"Updated 14:05" or "Offline · updated 14:05", depending on whether the data is a cached copy.</summary>
        private string BuildUpdatedLabel(WeatherForecastData data)
        {
            string time = data.FetchedAtUtc.ToLocalTime().ToString("t", _i18nService.CurrentFormatProvider);
            string template = data.IsStale
                ? _i18nService.GetString("OfflineUpdatedAt", "Offline · updated {0}")
                : _i18nService.GetString("UpdatedAt", "Updated {0}");
            return string.Format(template, time);
        }

        private void ScheduleStartupRetry(int delaySeconds)
        {
            var retryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(delaySeconds) };
            retryTimer.Tick += (s, e) =>
            {
                retryTimer.Stop();
                if (!_hasLoadedOnce)
                {
                    _ = RefreshWeatherAsync(isStartup: true);
                }
            };
            retryTimer.Start();
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
            TxtPrecipitation.Text = $"{data.Current.PrecipitationProbability:F0}%";
            TxtPressure.Text = $"{data.Current.Pressure:F1} hPa";
            TxtAirQuality.Text = data.Current.AirQualityText;
            TxtUV.Text = data.Current.UvIndexText;
            TxtSunrise.Text = data.Current.SunriseTime;
            TxtSunset.Text = data.Current.SunsetTime;

            HourlyItemsControl.ItemsSource = data.HourlyForecast;

            string updatedLabel = BuildUpdatedLabel(data);
            TxtUpdated.Text = data.IsStale ? $"⚠ {updatedLabel}" : updatedLabel;

            string trayEmoji = WeatherHelper.GetTrayEmoji(data.Current.WeatherCode, data.Current.IsDay);
            // Stale marker goes first: the shell truncates long tooltips from the end.
            string trayTooltip = data.IsStale ? $"⚠ {updatedLabel}\n{data.Current.SummaryText}" : data.Current.SummaryText;
            _trayIconManager.Update(trayEmoji, trayTooltip);
        }

        private void TrayIconManager_TrayIconClicked(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                double msSinceDeactivated = (DateTime.UtcNow - _lastDeactivatedTime).TotalMilliseconds;
                if (msSinceDeactivated < 350)
                {
                    // Do not re-open when clicked while already open (Deactivated just hid it 1ms ago)
                    return;
                }

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
                    _lastDeactivatedTime = DateTime.UtcNow;
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
