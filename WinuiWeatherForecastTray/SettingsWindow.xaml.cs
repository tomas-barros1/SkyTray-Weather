using System;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WinuiWeatherForecastTray.Services;
using WinuiWeatherForecastTray.Services.Interfaces;
using WinRT.Interop;

namespace WinuiWeatherForecastTray
{
    public sealed partial class SettingsWindow : Window
    {
        // Window size in device-independent pixels; converted to physical pixels per monitor DPI.
        private const int WindowWidthDips = 360;
        private const int WindowHeightDips = 346;

        private readonly IAutostartService _autostartService;
        private readonly ISettingsService _settingsService;
        private readonly ILocationCacheService _locationCacheService;
        private readonly IForecastCacheService _forecastCacheService;
        private readonly II18nService _i18nService;
        private IntPtr _hwnd;
        private AppWindow _appWindow;
        private bool _isInitializing = true;

        public event EventHandler? SettingsChanged;

        /// <summary>Raised after the location and forecast caches were cleared.</summary>
        public event EventHandler? CacheCleared;

        public SettingsWindow(II18nService i18nService)
        {
            InitializeComponent();

            _i18nService = i18nService ?? new I18nService();
            _autostartService = new AutostartService();
            _settingsService = new SettingsService();
            _locationCacheService = new LocationCacheService();
            _forecastCacheService = new ForecastCacheService();

            _hwnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            ConfigureWindow();
            ApplyLocalization();
            LoadSettings();

            _isInitializing = false;
        }

        private void ConfigureWindow()
        {
            PopupWindowHelper.ApplyChrome(this, _hwnd, _appWindow);
            PopupWindowHelper.ResizeToDips(_hwnd, _appWindow, WindowWidthDips, WindowHeightDips);

            CenterOnScreen();
        }

        private void CenterOnScreen()
        {
            if (_hwnd == IntPtr.Zero) return;

            WindowId windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
            if (displayArea != null)
            {
                SizeInt32 size = _appWindow.Size;
                int x = displayArea.WorkArea.X + (displayArea.WorkArea.Width - size.Width) / 2;
                int y = displayArea.WorkArea.Y + (displayArea.WorkArea.Height - size.Height) / 2;

                _appWindow.Move(new PointInt32(x, y));
            }
        }

        private void ApplyLocalization()
        {
            TxtTitle.Text = _i18nService.GetString("SettingsTitle", "Settings");
            TxtStartWithWindows.Text = _i18nService.GetString("StartWithWindows", "Start with Windows");
            TxtRefreshInterval.Text = _i18nService.GetString("RefreshInterval", "Refresh interval:");
            TxtLocationCache.Text = _i18nService.GetString("LocationCache", "Location Cache:");
            TxtClearCache.Text = _i18nService.GetString("ClearCache", "Clear Cache");
            TxtHoverCard.Text = _i18nService.GetString("HoverCard", "Show weather card when hovering the tray icon");
        }

        private void LoadSettings()
        {
            ChkAutostart.IsChecked = _autostartService.IsAutostartEnabled();
            ChkHoverCard.IsChecked = _settingsService.GetHoverCardEnabled();

            int currentMinutes = _settingsService.GetRefreshIntervalMinutes();
            foreach (ComboBoxItem item in CmbRefreshInterval.Items)
            {
                if (item.Tag is string tagStr && int.TryParse(tagStr, out int val) && val == currentMinutes)
                {
                    CmbRefreshInterval.SelectedItem = item;
                    break;
                }
            }
        }

        private void ChkAutostart_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = ChkAutostart.IsChecked ?? false;
            _autostartService.SetAutostart(isChecked);
        }

        private void ChkHoverCard_Click(object sender, RoutedEventArgs e)
        {
            _settingsService.SetHoverCardEnabled(ChkHoverCard.IsChecked ?? true);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void CmbRefreshInterval_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            if (CmbRefreshInterval.SelectedItem is ComboBoxItem selected && selected.Tag is string tagStr && int.TryParse(tagStr, out int minutes))
            {
                _settingsService.SetRefreshIntervalMinutes(minutes);
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void BtnClearCache_Click(object sender, RoutedEventArgs e)
        {
            _locationCacheService.ClearCache();
            // The saved forecast belongs to the old location, so it must go too.
            _forecastCacheService.Clear();
            TxtCacheStatus.Text = _i18nService.GetString("CacheCleared", "Location cache cleared successfully!");
            TxtCacheStatus.Visibility = Visibility.Visible;
            CacheCleared?.Invoke(this, EventArgs.Empty);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
