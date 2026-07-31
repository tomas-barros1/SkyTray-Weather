using Microsoft.UI.Xaml;

namespace WinuiWheaterForecastTray
{
    public partial class App : Application
    {
        private Window? _window;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            // App starts quietly in the tray without stealing focus on startup
        }
    }
}
