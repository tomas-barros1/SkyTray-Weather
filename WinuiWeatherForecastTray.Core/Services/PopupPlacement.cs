using System;

namespace WinuiWeatherForecastTray.Services;

/// <summary>
/// Pure geometry for popups that open next to the tray icon (hover card, context menu), kept free of UI types so it
/// can be unit-tested. All values are in the same unit (physical pixels).
/// </summary>
public static class PopupPlacement
{
    /// <summary>
    /// Centers the popup horizontally on the anchor and puts it above it for a bottom taskbar (below for a top one),
    /// then keeps it inside the work area, <paramref name="margin"/> away from the edges.
    /// </summary>
    /// <returns>Top-left corner of the popup.</returns>
    public static (int X, int Y) Calculate(
        int anchorLeft, int anchorTop, int anchorRight, int anchorBottom,
        int workX, int workY, int workWidth, int workHeight,
        int width, int height, int gap, int margin)
    {
        int minX = workX + margin;
        int maxX = Math.Max(minX, workX + workWidth - width - margin);
        int x = Math.Clamp((anchorLeft + anchorRight) / 2 - width / 2, minX, maxX);

        // A bottom taskbar puts the icon in the lower half of the screen, a top one in the upper half.
        bool above = (anchorTop + anchorBottom) / 2 >= workY + workHeight / 2;
        int y = above ? anchorTop - height - gap : anchorBottom + gap;

        int minY = workY + margin;
        int maxY = Math.Max(minY, workY + workHeight - height - margin);
        y = Math.Clamp(y, minY, maxY);

        return (x, y);
    }
}
