using FluentAssertions;
using WinuiWeatherForecastTray.Services;
using Xunit;
using static WinuiWeatherForecastTray.Services.HoverIntentTracker;

namespace WinuiWeatherForecastTray.Tests;

public class HoverIntentTrackerTests
{
    private static readonly TimeSpan ShowDelay = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan HideGrace = TimeSpan.FromMilliseconds(350);
    private static readonly DateTime T0 = new(2026, 11, 13, 12, 0, 0, DateTimeKind.Utc);

    private static HoverIntentTracker NewTracker() => new(ShowDelay, HideGrace);

    private static DateTime At(int ms) => T0.AddMilliseconds(ms);

    [Fact]
    public void Hover_ShowsCardOnlyAfterTheDelay()
    {
        var tracker = NewTracker();
        tracker.NotifyHover(At(0), canShow: true);

        tracker.IsPending.Should().BeTrue();
        tracker.Tick(At(100), cursorInside: true, canShow: true).Should().Be(HoverAction.None);
        tracker.Tick(At(299), cursorInside: true, canShow: true).Should().Be(HoverAction.None);
        tracker.Tick(At(300), cursorInside: true, canShow: true).Should().Be(HoverAction.Show);

        tracker.IsVisible.Should().BeTrue();
        tracker.IsPending.Should().BeFalse();
    }

    [Fact]
    public void Hover_FlyByThatLeavesBeforeTheDelay_NeverShowsTheCard()
    {
        var tracker = NewTracker();
        tracker.NotifyHover(At(0), canShow: true);

        tracker.Tick(At(100), cursorInside: false, canShow: true).Should().Be(HoverAction.None);
        tracker.Tick(At(400), cursorInside: false, canShow: true).Should().Be(HoverAction.None);

        tracker.IsVisible.Should().BeFalse();
        tracker.NeedsTimer.Should().BeFalse();
    }

    [Fact]
    public void VisibleCard_StaysWhileTheCursorIsOnTheIconOrTheCard()
    {
        var tracker = NewTracker();
        tracker.NotifyHover(At(0), canShow: true);
        tracker.Tick(At(300), cursorInside: true, canShow: true);

        for (int ms = 400; ms <= 5000; ms += 100)
        {
            tracker.Tick(At(ms), cursorInside: true, canShow: true).Should().Be(HoverAction.None);
        }

        tracker.IsVisible.Should().BeTrue();
    }

    [Fact]
    public void VisibleCard_HidesOnlyAfterTheGracePeriodOutsideIconAndCard()
    {
        var tracker = NewTracker();
        tracker.NotifyHover(At(0), canShow: true);
        tracker.Tick(At(300), cursorInside: true, canShow: true);
        tracker.Tick(At(400), cursorInside: true, canShow: true); // last time inside

        tracker.Tick(At(500), cursorInside: false, canShow: true).Should().Be(HoverAction.None);
        tracker.Tick(At(750), cursorInside: false, canShow: true).Should().Be(HoverAction.None);   // 350 ms: not yet "more than" the grace
        tracker.Tick(At(760), cursorInside: false, canShow: true).Should().Be(HoverAction.Hide);

        tracker.IsVisible.Should().BeFalse();
        tracker.NeedsTimer.Should().BeFalse();
    }

    [Fact]
    public void CrossingTheGapBetweenIconAndCard_KeepsTheCardOpen()
    {
        var tracker = NewTracker();
        tracker.NotifyHover(At(0), canShow: true);
        tracker.Tick(At(300), cursorInside: true, canShow: true);

        // cursor briefly outside both (in the gap), then lands on the card
        tracker.Tick(At(400), cursorInside: false, canShow: true).Should().Be(HoverAction.None);
        tracker.Tick(At(500), cursorInside: false, canShow: true).Should().Be(HoverAction.None);
        tracker.Tick(At(600), cursorInside: true, canShow: true).Should().Be(HoverAction.None);
        tracker.Tick(At(900), cursorInside: true, canShow: true).Should().Be(HoverAction.None);

        tracker.IsVisible.Should().BeTrue();
    }

    [Fact]
    public void WhenTheCardMustNotShow_NothingIsScheduled()
    {
        var tracker = NewTracker();

        tracker.NotifyHover(At(0), canShow: false);

        tracker.IsPending.Should().BeFalse();
        tracker.NeedsTimer.Should().BeFalse();
    }

    [Fact]
    public void CanShowTurningFalse_HidesAVisibleCardImmediately()
    {
        var tracker = NewTracker();
        tracker.NotifyHover(At(0), canShow: true);
        tracker.Tick(At(300), cursorInside: true, canShow: true);

        // e.g. the panel was opened by clicking the icon
        tracker.Tick(At(350), cursorInside: true, canShow: false).Should().Be(HoverAction.Hide);

        tracker.IsVisible.Should().BeFalse();
    }

    [Fact]
    public void CanShowTurningFalse_CancelsAPendingShow()
    {
        var tracker = NewTracker();
        tracker.NotifyHover(At(0), canShow: true);

        tracker.Tick(At(100), cursorInside: true, canShow: false).Should().Be(HoverAction.None);

        tracker.IsPending.Should().BeFalse();
        tracker.Tick(At(400), cursorInside: true, canShow: true).Should().Be(HoverAction.None); // nothing pending any more
    }

    [Fact]
    public void Dismiss_ReportsWhetherTheCardWasVisible_AndResets()
    {
        var tracker = NewTracker();
        tracker.Dismiss().Should().BeFalse();

        tracker.NotifyHover(At(0), canShow: true);
        tracker.Tick(At(300), cursorInside: true, canShow: true);

        tracker.Dismiss().Should().BeTrue();
        tracker.IsVisible.Should().BeFalse();
        tracker.NeedsTimer.Should().BeFalse();
    }

    [Fact]
    public void AfterBeingHidden_HoveringAgainStartsANewDelay()
    {
        var tracker = NewTracker();
        tracker.NotifyHover(At(0), canShow: true);
        tracker.Tick(At(300), cursorInside: true, canShow: true);
        tracker.Tick(At(800), cursorInside: false, canShow: true); // hides

        tracker.NotifyHover(At(2000), canShow: true);

        tracker.Tick(At(2100), cursorInside: true, canShow: true).Should().Be(HoverAction.None);
        tracker.Tick(At(2300), cursorInside: true, canShow: true).Should().Be(HoverAction.Show);
    }
}
