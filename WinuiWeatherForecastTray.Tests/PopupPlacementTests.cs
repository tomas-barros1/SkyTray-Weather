using FluentAssertions;
using WinuiWeatherForecastTray.Services;
using Xunit;

namespace WinuiWeatherForecastTray.Tests;

public class PopupPlacementTests
{
    // 1920x1080 screen with a 48 px taskbar at the bottom: work area is 1920x1032.
    private const int WorkX = 0, WorkY = 0, WorkW = 1920, WorkH = 1032;
    private const int Gap = 8, Margin = 8;

    private static (int X, int Y) Place(int left, int top, int right, int bottom, int width = 300, int height = 228,
        int workX = WorkX, int workY = WorkY, int workW = WorkW, int workH = WorkH) =>
        PopupPlacement.Calculate(left, top, right, bottom, workX, workY, workW, workH, width, height, Gap, Margin);

    [Fact]
    public void BottomTaskbar_PopupIsCenteredOnTheIconAndSitsAboveTheTaskbar()
    {
        // 24 px icon at x=1500..1524 inside the taskbar (y=1044..1068)
        var (x, y) = Place(1500, 1044, 1524, 1068);

        x.Should().Be(1512 - 150);                   // centered on the icon
        y.Should().Be(WorkY + WorkH - 228 - Margin); // ends one margin above the taskbar, never overlapping it
    }

    [Fact]
    public void TopTaskbar_PopupIsPlacedBelowTheTaskbar()
    {
        // taskbar at the top: work area starts at y=48, the icon is inside the taskbar (y=12..36)
        var (x, y) = Place(1500, 12, 1524, 36, workY: 48, workH: 1032);

        y.Should().Be(48 + Margin);   // starts one margin below the taskbar, never overlapping it
        x.Should().Be(1512 - 150);
    }

    [Fact]
    public void IconInsideTheWorkArea_PopupSitsAboveItWithTheGap()
    {
        // e.g. a floating overflow flyout: the icon is well inside the work area, so the gap applies
        var (_, y) = Place(900, 700, 924, 724);

        y.Should().Be(700 - 228 - Gap);
    }

    [Fact]
    public void IconNearTheRightEdge_PopupIsClampedInsideTheWorkArea()
    {
        var (x, _) = Place(1890, 1044, 1914, 1068);

        x.Should().Be(WorkW - 300 - Margin);
    }

    [Fact]
    public void IconNearTheLeftEdge_PopupIsClampedInsideTheWorkArea()
    {
        var (x, _) = Place(4, 1044, 28, 1068);

        x.Should().Be(WorkX + Margin);
    }

    [Fact]
    public void PopupTallerThanTheSpaceAbove_IsClampedToTheTopMargin()
    {
        // tiny work area: the popup cannot fit above the icon
        var (_, y) = Place(500, 190, 524, 214, height: 228, workH: 200);

        y.Should().Be(WorkY + Margin);
    }

    [Fact]
    public void PopupWiderThanTheWorkArea_DoesNotThrowAndStartsAtTheLeftMargin()
    {
        var (x, _) = Place(100, 1044, 124, 1068, width: 400, workW: 300);

        x.Should().Be(WorkX + Margin);
    }

    [Fact]
    public void MonitorWithNegativeOrigin_IsHandled()
    {
        // secondary monitor to the left of the primary one: work area starts at x=-1920
        var (x, y) = Place(-300, 1044, -276, 1068, workX: -1920);

        x.Should().Be(-288 - 150);
        y.Should().Be(WorkY + WorkH - 228 - Margin);
    }
}
