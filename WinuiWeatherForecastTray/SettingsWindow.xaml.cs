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
using WinuiWeatherForecastTray.Core.Services;
using WinRT.Interop;

namespace WinuiWeatherForecastTray
{
    public sealed partial class SettingsWindow : Window
    {
        // Window size in device-independent pixels; converted to physical pixels per monitor DPI.
        private const int WindowWidthDips = 360;
        private const int WindowHeightDips = 500;

        private readonly IAutostartService _autostartService;
        private readonly ISettingsService _settingsService;
        private readonly ILocationCacheService _locationCacheService;
        private readonly IForecastCacheService _forecastCacheService;
        private readonly II18nService _i18nService;
        private readonly AppUpdateService _updateService;
        private GitHubUpdateChecker.AvailableUpdate? _availableUpdate;
        private bool _updateOperationInProgress;
        private IntPtr _hwnd;
        private AppWindow _appWindow;
        private bool _isInitializing = true;

        public event EventHandler? SettingsChanged;
        public event EventHandler? AutoUpdateSettingChanged;

        /// <summary>Raised after the location and forecast caches were cleared.</summary>
        public event EventHandler? CacheCleared;
        public event Action<GitHubUpdateChecker.AvailableUpdate?>? UpdateAvailabilityChanged;
        public event EventHandler? UpdateInstallStarted;

        internal SettingsWindow(II18nService i18nService, ISettingsService settingsService, AppUpdateService updateService,
            GitHubUpdateChecker.AvailableUpdate? availableUpdate = null)
        {
            InitializeComponent();

            _i18nService = i18nService ?? new I18nService();
            _autostartService = new AutostartService();
            _settingsService = settingsService;
            _updateService = updateService;
            _availableUpdate = availableUpdate;
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
            TxtAutoUpdate.Text = _i18nService.GetString("AutoUpdateEnabled", "Check for updates automatically");
            TxtCheckUpdates.Text = _i18nService.GetString("UpdateCheckNow", "Check now");
            TxtInstallUpdate.Text = _i18nService.GetString("UpdateDownloadInstall", "Download and install");
        }

        private void LoadSettings()
        {
            ChkAutostart.IsChecked = _autostartService.IsAutostartEnabled();
            ChkHoverCard.IsChecked = _settingsService.GetHoverCardEnabled();
            ChkAutoUpdate.IsChecked = _settingsService.GetAutoUpdateEnabled();
            if (_availableUpdate is not null)
                SetAvailableUpdate(_availableUpdate);
            else
                TxtUpdateStatus.Text = string.Empty;

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

        private void ChkAutoUpdate_Click(object sender, RoutedEventArgs e)
        {
            _settingsService.SetAutoUpdateEnabled(ChkAutoUpdate.IsChecked ?? true);
            AutoUpdateSettingChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetAvailableUpdate(GitHubUpdateChecker.AvailableUpdate? update)
        {
            _availableUpdate = update;
            BtnInstallUpdate.Visibility = update is null ? Visibility.Collapsed : Visibility.Visible;
            TxtUpdateStatus.Text = update is null
                ? _i18nService.GetString("UpdateStatusCurrent", "You are up to date")
                : string.Format(_i18nService.GetString("UpdateStatusAvailable", "Version {0} available"), update.Version);
        }

        public void SetUpdateCheckFailed()
        {
            TxtUpdateStatus.Text = _i18nService.GetString("UpdateStatusFailed", "Could not check for updates");
        }

        private async void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            if (_updateOperationInProgress)
                return;

            if (_updateService.IsDevelopmentBuild)
            {
                SetAvailableUpdate(null);
                UpdateAvailabilityChanged?.Invoke(null);
                return;
            }

            DateTime now = DateTime.UtcNow;
            if (!UpdateCheckPolicy.CanCheckNow(now, _settingsService.GetLastUpdateCheckUtc()))
            {
                TxtUpdateStatus.Text = _i18nService.GetString("UpdateStatusRecentlyChecked", "Updates were checked recently");
                return;
            }

            SetUpdateCheckInProgress(true);
            TxtUpdateError.Visibility = Visibility.Collapsed;
            TxtUpdateStatus.Text = _i18nService.GetString("UpdateStatusChecking", "Checking for updates...");
            _settingsService.SetLastUpdateCheckUtc(now);
            try
            {
                GitHubUpdateChecker.AvailableUpdate? update = await _updateService.CheckForUpdateAsync();
                SetAvailableUpdate(update);
                UpdateAvailabilityChanged?.Invoke(update);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update check failed: {ex}");
                SetUpdateCheckFailed();
            }
            finally
            {
                SetUpdateCheckInProgress(false);
            }
        }

        private async void BtnInstallUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (_availableUpdate is null || _updateOperationInProgress)
                return;

            var confirmation = new ContentDialog
            {
                Title = _i18nService.GetString("UpdateAvailableTitle", "Update available"),
                Content = string.Format(
                    _i18nService.GetString("UpdateConfirmInstall", "Download and verify version {0}, then install it?"),
                    _availableUpdate.Version),
                PrimaryButtonText = _i18nService.GetString("UpdateDownloadInstall", "Download and install"),
                CloseButtonText = _i18nService.GetString("UpdateLater", "Later"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = RootGrid.XamlRoot
            };
            if (await confirmation.ShowAsync() != ContentDialogResult.Primary)
                return;

            SetUpdateCheckInProgress(true);
            TxtUpdateError.Visibility = Visibility.Collapsed;
            TxtUpdateStatus.Text = _i18nService.GetString("UpdateDownloading", "Downloading update...");
            PrgUpdateDownload.Visibility = Visibility.Visible;
            PrgUpdateDownload.Value = 0;
            var progress = new Progress<double>(value => PrgUpdateDownload.Value = value * 100);
            try
            {
                bool started = await _updateService.InstallUpdateAsync(_availableUpdate, progress);
                if (started)
                {
                    UpdateInstallStarted?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    TxtUpdateStatus.Text = _i18nService.GetString("UpdateChecksumsMissing", "Checksums are unavailable. Opening the release page.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update install failed: {ex}");
                TxtUpdateError.Text = _i18nService.GetString("UpdateInstallError", "Could not verify or install the update");
                TxtUpdateError.Visibility = Visibility.Visible;
                TxtUpdateStatus.Text = _i18nService.GetString("UpdateStatusFailed", "Could not check for updates");
            }
            finally
            {
                PrgUpdateDownload.Visibility = Visibility.Collapsed;
                SetUpdateCheckInProgress(false);
            }
        }

        private void SetUpdateCheckInProgress(bool inProgress)
        {
            _updateOperationInProgress = inProgress;
            BtnCheckUpdates.IsEnabled = !inProgress;
            BtnInstallUpdate.IsEnabled = !inProgress;
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
