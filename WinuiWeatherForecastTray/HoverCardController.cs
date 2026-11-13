using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using WinuiWeatherForecastTray.Models;
using WinuiWeatherForecastTray.Native;
using WinuiWeatherForecastTray.Services;
using WinuiWeatherForecastTray.Services.Interfaces;
using WinuiWeatherForecastTray.Tray;

namespace WinuiWeatherForecastTray
{
    /// <summary>
    /// Wires the tray icon's mouse events to the <see cref="HoverCardWindow"/>. The "when do we show/hide" decisions
    /// live in <see cref="HoverIntentTracker"/> (unit-tested); this class only supplies the real inputs: the cursor
    /// position, the icon rectangle and a ~100 ms polling timer (the tray reports mouse movement but never "mouse left").
    /// </summary>
    internal sealed class HoverCardController : IDisposable
    {
        private static readonly TimeSpan ShowDelay = TimeSpan.FromMilliseconds(300);
        private static readonly TimeSpan HideGrace = TimeSpan.FromMilliseconds(350);
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

        /// <summary>Extra pixels around the icon that still count as "on the icon".</summary>
        private const int IconSlackPixels = 2;

        /// <summary>When the shell cannot report the icon rectangle, the cursor may drift this far from where hovering started.</summary>
        private const int FallbackRadiusPixels = 24;

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        private readonly HoverCardWindow _window;
        private readonly TrayIconManager _tray;
        private readonly Func<bool> _isPanelVisible;
        private readonly DispatcherQueueTimer _timer;
        private readonly HoverIntentTracker _intent = new(ShowDelay, HideGrace);

        private bool _enabled = true;
        private bool _hasData;
        private POINT _hoverAnchor;

        public HoverCardController(DispatcherQueue dispatcherQueue, II18nService i18nService, TrayIconManager tray, Func<bool> isPanelVisible)
        {
            _tray = tray;
            _isPanelVisible = isPanelVisible;
            _window = new HoverCardWindow(i18nService);

            _timer = dispatcherQueue.CreateTimer();
            _timer.Interval = PollInterval;
            _timer.Tick += (s, e) => OnTick();
        }

        /// <summary>Turns the feature on or off; turning it off hides a visible card.</summary>
        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            if (!enabled)
            {
                Dismiss();
            }
        }

        /// <summary>Gives the card the latest forecast (also while hidden, so it is ready when the user hovers).</summary>
        public void Update(WeatherForecastData data)
        {
            _window.Update(data);
            _hasData = true;
        }

        /// <summary>The mouse moved over the tray icon.</summary>
        public void NotifyHover()
        {
            GetCursorPos(out _hoverAnchor);
            _intent.NotifyHover(DateTime.UtcNow, CanShow());

            if (_intent.NeedsTimer && !_timer.IsRunning)
            {
                _timer.Start();
            }
        }

        /// <summary>Hides the card right away (click on the icon, panel opened, app exiting...).</summary>
        public void Dismiss()
        {
            if (_intent.Dismiss())
            {
                _window.Hide();
            }

            _timer.Stop();
        }

        private bool CanShow() => _enabled && _hasData && !_isPanelVisible();

        private void OnTick()
        {
            HoverIntentTracker.HoverAction action = _intent.Tick(DateTime.UtcNow, IsCursorOverIconOrCard(), CanShow());

            switch (action)
            {
                case HoverIntentTracker.HoverAction.Show:
                    _window.ShowNear(GetAnchorRect());
                    break;
                case HoverIntentTracker.HoverAction.Hide:
                    _window.Hide();
                    break;
            }

            if (!_intent.NeedsTimer)
            {
                _timer.Stop();
            }
        }

        private bool IsCursorOverIconOrCard()
        {
            GetCursorPos(out POINT cursor);

            if (_window.IsShown && Contains(_window.Bounds.X, _window.Bounds.Y, _window.Bounds.Width, _window.Bounds.Height, cursor))
                return true;

            if (_tray.TryGetIconRect(out RECT icon))
            {
                return Contains(icon.Left - IconSlackPixels, icon.Top - IconSlackPixels,
                    icon.Width + 2 * IconSlackPixels, icon.Height + 2 * IconSlackPixels, cursor);
            }

            return Math.Abs(cursor.X - _hoverAnchor.X) <= FallbackRadiusPixels
                && Math.Abs(cursor.Y - _hoverAnchor.Y) <= FallbackRadiusPixels;
        }

        /// <summary>The icon's rectangle, or a small square around the cursor if the shell cannot tell.</summary>
        private RECT GetAnchorRect()
        {
            if (_tray.TryGetIconRect(out RECT icon))
                return icon;

            return new RECT
            {
                Left = _hoverAnchor.X - FallbackRadiusPixels / 2,
                Right = _hoverAnchor.X + FallbackRadiusPixels / 2,
                Top = _hoverAnchor.Y - FallbackRadiusPixels / 2,
                Bottom = _hoverAnchor.Y + FallbackRadiusPixels / 2
            };
        }

        private static bool Contains(int x, int y, int width, int height, POINT p) =>
            p.X >= x && p.X < x + width && p.Y >= y && p.Y < y + height;

        public void Dispose()
        {
            _timer.Stop();
            _window.Close();
        }
    }
}
