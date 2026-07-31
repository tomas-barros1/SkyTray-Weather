using System;
using System.Runtime.InteropServices;
using WinuiWheaterForecastTray.Native;

namespace WinuiWheaterForecastTray.Tray;

public static class TrayVectorRenderer
{
    private const int SmoothingModeAntiAlias = 4;

    [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipSetSmoothingMode(IntPtr graphics, int smoothingMode);

    [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipCreateSolidFill(uint color, out IntPtr brush);

    [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipDeleteBrush(IntPtr brush);

    [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipCreatePen1(uint color, float width, int unit, out IntPtr pen);

    [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipDeletePen(IntPtr pen);

    [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipFillEllipseI(IntPtr graphics, IntPtr brush, int x, int y, int width, int height);

    [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipDrawLineI(IntPtr graphics, IntPtr pen, int x1, int y1, int x2, int y2);

    [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipFillPolygonI(IntPtr graphics, IntPtr brush, GdipPointI[] points, int count, int fillMode);

    [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipFillRectangleI(IntPtr graphics, IntPtr brush, int x, int y, int width, int height);

    public static void RenderWeatherIcon(IntPtr graphics, string emojiOrCode)
    {
        GdipSetSmoothingMode(graphics, SmoothingModeAntiAlias);

        string code = emojiOrCode ?? "☀️";

        if (code.Contains("☀️") || code.Equals("sun", StringComparison.OrdinalIgnoreCase))
        {
            DrawSun(graphics);
        }
        else if (code.Contains("🌙") || code.Equals("night", StringComparison.OrdinalIgnoreCase))
        {
            DrawMoon(graphics);
        }
        else if (code.Contains("⛅") || code.Equals("partly_cloudy", StringComparison.OrdinalIgnoreCase))
        {
            DrawPartlyCloudy(graphics);
        }
        else if (code.Contains("🌧️") || code.Equals("rain", StringComparison.OrdinalIgnoreCase))
        {
            DrawRain(graphics);
        }
        else if (code.Contains("⛈️") || code.Equals("thunderstorm", StringComparison.OrdinalIgnoreCase))
        {
            DrawThunderstorm(graphics);
        }
        else if (code.Contains("❄️") || code.Equals("snow", StringComparison.OrdinalIgnoreCase))
        {
            DrawSnow(graphics);
        }
        else
        {
            DrawCloudy(graphics);
        }
    }

    private static void DrawSun(IntPtr graphics)
    {
        uint goldColor = 0xFFFFC107; // Bright Gold ARGB
        uint rayColor = 0xFFFFD54F;  // Light Gold ARGB

        GdipCreateSolidFill(goldColor, out IntPtr goldBrush);
        GdipCreatePen1(rayColor, 2.5f, 2, out IntPtr rayPen);

        // Sun Rays (8 directions)
        int cx = 16, cy = 16;
        int r1 = 9, r2 = 14;

        GdipDrawLineI(graphics, rayPen, cx, cy - r2, cx, cy - r1);
        GdipDrawLineI(graphics, rayPen, cx, cy + r1, cx, cy + r2);
        GdipDrawLineI(graphics, rayPen, cx - r2, cy, cx - r1, cy);
        GdipDrawLineI(graphics, rayPen, cx + r1, cy, cx + r2, cy);

        GdipDrawLineI(graphics, rayPen, cx - 10, cy - 10, cx - 6, cy - 6);
        GdipDrawLineI(graphics, rayPen, cx + 6, cy + 6, cx + 10, cy + 10);
        GdipDrawLineI(graphics, rayPen, cx + 6, cy - 6, cx + 10, cy - 10);
        GdipDrawLineI(graphics, rayPen, cx - 10, cy + 10, cx - 6, cy + 6);

        // Core Sun Circle
        GdipFillEllipseI(graphics, goldBrush, 9, 9, 14, 14);

        GdipDeleteBrush(goldBrush);
        GdipDeletePen(rayPen);
    }

    private static void DrawMoon(IntPtr graphics)
    {
        uint moonColor = 0xFFFFD54F;
        uint starColor = 0xFFFFF59D;

        GdipCreateSolidFill(moonColor, out IntPtr moonBrush);
        GdipCreateSolidFill(starColor, out IntPtr starBrush);

        // Crescent moon polygon/ellipses
        GdipFillEllipseI(graphics, moonBrush, 6, 6, 18, 18);

        // Erase inner circle for crescent effect (transparent subtractive ellipse)
        GdipCreateSolidFill(0x00000000, out IntPtr clearBrush);
        // Note: Using a offset circle with moon color background cut
        GdipFillEllipseI(graphics, clearBrush, 11, 4, 15, 15);
        GdipDeleteBrush(clearBrush);

        // Stars
        GdipFillEllipseI(graphics, starBrush, 23, 7, 3, 3);
        GdipFillEllipseI(graphics, starBrush, 26, 16, 2, 2);

        GdipDeleteBrush(moonBrush);
        GdipDeleteBrush(starBrush);
    }

    private static void DrawPartlyCloudy(IntPtr graphics)
    {
        // Small Sun top right
        uint goldColor = 0xFFFFC107;
        GdipCreateSolidFill(goldColor, out IntPtr goldBrush);
        GdipCreatePen1(goldColor, 2.0f, 2, out IntPtr sunPen);

        GdipDrawLineI(graphics, sunPen, 22, 3, 22, 6);
        GdipDrawLineI(graphics, sunPen, 27, 8, 30, 8);
        GdipDrawLineI(graphics, sunPen, 26, 4, 28, 2);
        GdipFillEllipseI(graphics, goldBrush, 17, 4, 10, 10);

        GdipDeleteBrush(goldBrush);
        GdipDeletePen(sunPen);

        // Cloud overlay bottom left
        DrawCloudShapes(graphics, 0xFFFFFFFF, 0xFFCFD8DC, 0, 4);
    }

    private static void DrawCloudy(IntPtr graphics)
    {
        DrawCloudShapes(graphics, 0xFFFFFFFF, 0xFFB0BEC5, 0, 0);
    }

    private static void DrawRain(IntPtr graphics)
    {
        DrawCloudShapes(graphics, 0xFFECEFF1, 0xFF90A4AE, 0, -2);

        // Rain drops
        uint rainColor = 0xFF00B0FF; // Bright Cyan Blue
        GdipCreatePen1(rainColor, 2.5f, 2, out IntPtr rainPen);

        GdipDrawLineI(graphics, rainPen, 10, 23, 8, 29);
        GdipDrawLineI(graphics, rainPen, 16, 23, 14, 29);
        GdipDrawLineI(graphics, rainPen, 22, 23, 20, 29);

        GdipDeletePen(rainPen);
    }

    private static void DrawThunderstorm(IntPtr graphics)
    {
        DrawCloudShapes(graphics, 0xFFB0BEC5, 0xFF607D8B, 0, -3);

        // Yellow Lightning Bolt
        uint yellowColor = 0xFFFFD600;
        GdipCreateSolidFill(yellowColor, out IntPtr yellowBrush);

        GdipPointI[] bolt = new GdipPointI[]
        {
            new GdipPointI(17, 17),
            new GdipPointI(12, 23),
            new GdipPointI(16, 23),
            new GdipPointI(13, 30),
            new GdipPointI(21, 22),
            new GdipPointI(17, 22)
        };

        GdipFillPolygonI(graphics, yellowBrush, bolt, 6, 0);
        GdipDeleteBrush(yellowBrush);
    }

    private static void DrawSnow(IntPtr graphics)
    {
        DrawCloudShapes(graphics, 0xFFE1F5FE, 0xFFB3E5FC, 0, -2);

        // Snowflake dots
        uint snowColor = 0xFFFFFFFF;
        GdipCreateSolidFill(snowColor, out IntPtr snowBrush);

        GdipFillEllipseI(graphics, snowBrush, 9, 24, 3, 3);
        GdipFillEllipseI(graphics, snowBrush, 15, 26, 4, 4);
        GdipFillEllipseI(graphics, snowBrush, 21, 24, 3, 3);

        GdipDeleteBrush(snowBrush);
    }

    private static void DrawCloudShapes(IntPtr graphics, uint topColor, uint shadowColor, int offsetX, int offsetY)
    {
        GdipCreateSolidFill(shadowColor, out IntPtr shadowBrush);
        GdipCreateSolidFill(topColor, out IntPtr topBrush);

        // Base shadow cloud
        int y = 14 + offsetY;
        GdipFillRectangleI(graphics, shadowBrush, 5 + offsetX, y + 4, 22, 8);
        GdipFillEllipseI(graphics, shadowBrush, 4 + offsetX, y, 12, 12);
        GdipFillEllipseI(graphics, shadowBrush, 12 + offsetX, y - 4, 13, 13);
        GdipFillEllipseI(graphics, shadowBrush, 18 + offsetX, y + 2, 10, 10);

        // Top cloud highlight
        GdipFillRectangleI(graphics, topBrush, 6 + offsetX, y + 3, 20, 7);
        GdipFillEllipseI(graphics, topBrush, 5 + offsetX, y, 10, 10);
        GdipFillEllipseI(graphics, topBrush, 13 + offsetX, y - 3, 11, 11);
        GdipFillEllipseI(graphics, topBrush, 19 + offsetX, y + 1, 8, 8);

        GdipDeleteBrush(shadowBrush);
        GdipDeleteBrush(topBrush);
    }
}
