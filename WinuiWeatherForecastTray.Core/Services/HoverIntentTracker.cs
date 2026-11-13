using System;

namespace WinuiWeatherForecastTray.Services;

/// <summary>
/// The decision logic behind the tray hover card, free of any UI so it can be unit-tested with a fake clock.
/// The UI feeds it two inputs — "the mouse moved over the icon" and a periodic tick with "is the cursor still over
/// the icon or the card" — and it answers when to show or hide the card:
/// show only after the cursor has rested on the icon for <c>showDelay</c> (so fly-bys do not pop up a card), and
/// hide once the cursor has been away from both the icon and the card for <c>hideGrace</c> (so the short gap
/// between icon and card can be crossed).
/// </summary>
public sealed class HoverIntentTracker
{
    public enum HoverAction
    {
        None,
        Show,
        Hide
    }

    private readonly TimeSpan _showDelay;
    private readonly TimeSpan _hideGrace;
    private DateTime _hoverStartedUtc;
    private DateTime _lastInsideUtc;

    public HoverIntentTracker(TimeSpan showDelay, TimeSpan hideGrace)
    {
        _showDelay = showDelay;
        _hideGrace = hideGrace;
    }

    /// <summary>The cursor rested on the icon but the card is not shown yet.</summary>
    public bool IsPending { get; private set; }

    /// <summary>The card is currently shown.</summary>
    public bool IsVisible { get; private set; }

    /// <summary>A polling timer is needed while a show is pending or the card is visible.</summary>
    public bool NeedsTimer => IsPending || IsVisible;

    /// <summary>Call whenever the mouse moves over the tray icon.</summary>
    /// <param name="canShow"><c>false</c> when the card must not appear (feature off, panel open, no data yet).</param>
    public void NotifyHover(DateTime nowUtc, bool canShow)
    {
        _lastInsideUtc = nowUtc;

        if (!canShow || IsVisible || IsPending)
            return;

        IsPending = true;
        _hoverStartedUtc = nowUtc;
    }

    /// <summary>Call periodically (~100 ms) while <see cref="NeedsTimer"/> is true.</summary>
    public HoverAction Tick(DateTime nowUtc, bool cursorInside, bool canShow)
    {
        if (!canShow)
            return Dismiss() ? HoverAction.Hide : HoverAction.None;

        if (cursorInside)
            _lastInsideUtc = nowUtc;

        if (IsPending)
        {
            if (!cursorInside)
            {
                IsPending = false; // moved away before the delay elapsed
                return HoverAction.None;
            }

            if (nowUtc - _hoverStartedUtc >= _showDelay)
            {
                IsPending = false;
                IsVisible = true;
                return HoverAction.Show;
            }

            return HoverAction.None;
        }

        if (IsVisible && nowUtc - _lastInsideUtc > _hideGrace)
        {
            IsVisible = false;
            return HoverAction.Hide;
        }

        return HoverAction.None;
    }

    /// <summary>Forgets any pending/visible state (click, panel opened, feature turned off).</summary>
    /// <returns><c>true</c> if the card was visible and must now be hidden.</returns>
    public bool Dismiss()
    {
        bool wasVisible = IsVisible;
        IsPending = false;
        IsVisible = false;
        return wasVisible;
    }
}
