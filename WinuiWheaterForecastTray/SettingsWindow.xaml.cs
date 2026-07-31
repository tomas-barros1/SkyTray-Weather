using System;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WinuiWheaterForecastTray.Services;
using WinuiWheaterForecastTray.Services.Interfaces;
using WinRT.Interop;

namespace WinuiWheaterForecastTray
{
    public sealed partial class SettingsWindow : Window
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        private readonly IAutostartService _autostartService;
        private readonly ISettingsService _settingsService;
        private readonly II18nService _i18nService;
        private IntPtr _hwnd;
        private AppWindow _appWindow;
        private bool _isInitializing = true;

        public event EventHandler? SettingsChanged;

        public SettingsWindow(II18nService i18nService)
        {
            InitializeComponent();

            _i18nService = i18nService ?? new I18nService();
            _autostartService = new AutostartService();
            _settingsService = new SettingsService();

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
            ExtendsContentIntoTitleBar = true;

            if (_appWindow != null)
            {
                _appWindow.Resize(new SizeInt32(350, 250));

                var presenter = _appWindow.Presenter as OverlappedPresenter;
                if (presenter != null)
                {
                    presenter.IsResizable = false;
                    presenter.IsMinimizable = false;
                    presenter.IsMaximizable = false;
                    presenter.IsAlwaysOnTop = true;
                    presenter.SetBorderAndTitleBar(false, false);
                }
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

            CenterOnScreen();
        }

        private void CenterOnScreen()
        {
            if (_hwnd == IntPtr.Zero) return;

            WindowId windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
            if (displayArea != null)
            {
                int width = 350;
                int height = 250;
                int x = displayArea.WorkArea.X + (displayArea.WorkArea.Width - width) / 2;
                int y = displayArea.WorkArea.Y + (displayArea.WorkArea.Height - height) / 2;

                _appWindow.Move(new PointInt32(x, y));
            }
        }

        private void ApplyLocalization()
        {
            TxtTitle.Text = _i18nService.GetString("SettingsTitle", "Settings");
            TxtStartWithWindows.Text = _i18nService.GetString("StartWithWindows", "Start with Windows");
            TxtRefreshInterval.Text = _i18nService.GetString("RefreshInterval", "Refresh interval:");
        }

        private void LoadSettings()
        {
            ChkAutostart.IsChecked = _autostartService.IsAutostartEnabled();

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

        private void CmbRefreshInterval_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            if (CmbRefreshInterval.SelectedItem is ComboBoxItem selected && selected.Tag is string tagStr && int.TryParse(tagStr, out int minutes))
            {
                _settingsService.SetRefreshIntervalMinutes(minutes);
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
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
