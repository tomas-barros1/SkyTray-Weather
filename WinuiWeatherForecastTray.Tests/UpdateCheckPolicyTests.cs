using System;
using FluentAssertions;
using WinuiWeatherForecastTray.Core.Services;
using Xunit;

namespace WinuiWeatherForecastTray.Tests;

public sealed class UpdateCheckPolicyTests
{
    [Fact]
    public void GetNextAutomaticCheckUtc_OnStartupSchedulesTwoMinutesAfterNow()
    {
        DateTime now = new(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);

        UpdateCheckPolicy.GetNextAutomaticCheckUtc(now, null, isStartup: true)
            .Should().Be(now.AddMinutes(2));
    }

    [Fact]
    public void GetNextAutomaticCheckUtc_EnforcesTwentyHourMinimum()
    {
        DateTime last = new(2026, 11, 18, 20, 0, 0, DateTimeKind.Utc);
        DateTime now = new(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);

        UpdateCheckPolicy.GetNextAutomaticCheckUtc(now, last, isStartup: true)
            .Should().Be(last.AddHours(20));
    }

    [Fact]
    public void GetNextAutomaticCheckUtc_AfterACompletedCheckSchedulesAboutSixHoursLater()
    {
        DateTime last = new(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);

        UpdateCheckPolicy.GetNextAutomaticCheckUtc(last, last, isStartup: false)
            .Should().Be(last.AddHours(20));
    }

    [Fact]
    public void CanCheckNow_RejectsCallsWithinTwentyHours()
    {
        DateTime last = new(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);

        UpdateCheckPolicy.CanCheckNow(last.AddHours(19), last).Should().BeFalse();
        UpdateCheckPolicy.CanCheckNow(last.AddHours(20), last).Should().BeTrue();
    }
}
