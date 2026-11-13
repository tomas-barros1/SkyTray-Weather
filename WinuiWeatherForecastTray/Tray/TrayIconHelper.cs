using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using WinuiWeatherForecastTray.Models;
using WinuiWeatherForecastTray.Native;

namespace WinuiWeatherForecastTray.Tray;

public static class TrayIconHelper
{
    private const int PixelFormat32bppARGB = 0x26200A;

    [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdiplusStartup(out IntPtr token, ref GdiplusStartupInput input, IntPtr output);

    [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdiplusShutdown(IntPtr token);

    [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipCreateBitmapFromScan0(int width, int height, int stride, int format, IntPtr scan0, out IntPtr bitmap);

    [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipGetImageGraphicsContext(IntPtr image, out IntPtr graphics);

    [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipGraphicsClear(IntPtr graphics, uint color);

    [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipCreateBitmapFromFile(string filename, out IntPtr bitmap);

    [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipCreateHICONFromBitmap(IntPtr bitmap, out IntPtr hicon);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForSystem();

    [DllImport("user32.dll")]
    private static extern int GetSystemMetricsForDpi(int nIndex, uint dpi);

    [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipDisposeImage(IntPtr image);

    [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipDeleteGraphics(IntPtr graphics);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyIcon(IntPtr hIcon);

    private const int SM_CXSMICON = 49;

    /// <summary>Pre-rendered PNG sizes in <c>Assets/WeatherIcons/png</c> (small-icon size at 100/125/150/200% scale).</summary>
    private static readonly int[] AssetSizes = { 16, 20, 24, 32 };

    private static IntPtr _gdiplusToken = IntPtr.Zero;
    private static readonly object LockObj = new();

    private static void EnsureGdiplus()
    {
        lock (LockObj)
        {
            if (_gdiplusToken == IntPtr.Zero)
            {
                var input = GdiplusStartupInput.Default;
                GdiplusStartup(out _gdiplusToken, ref input, IntPtr.Zero);
            }
        }
    }

    /// <summary>
    /// Creates the tray <c>HICON</c> for <paramref name="icon"/> from the PNG that matches the system's
    /// small-icon size (so it is not downscaled by the shell). If the asset is missing or cannot be loaded it
    /// falls back to the built-in GDI+ vector drawing, so the tray never ends up without an icon.
    /// </summary>
    public static IntPtr CreateIcon(WeatherIcon icon)
    {
        IntPtr hIcon = TryCreateIconFromAsset(icon);
        return hIcon != IntPtr.Zero ? hIcon : CreateEmojiIcon(FallbackCode(icon));
    }

    /// <summary>Smallest shipped PNG size that is at least <paramref name="requested"/> pixels (else the largest).</summary>
    internal static int PickAssetSize(int requested)
    {
        foreach (int size in AssetSizes)
        {
            if (size >= requested)
                return size;
        }

        return AssetSizes[^1];
    }

    private static IntPtr TryCreateIconFromAsset(WeatherIcon icon)
    {
        if (icon == WeatherIcon.Unknown)
            return IntPtr.Zero;

        try
        {
            EnsureGdiplus();

            int requested = GetSystemMetricsForDpi(SM_CXSMICON, GetDpiForSystem());
            int size = PickAssetSize(requested > 0 ? requested : 16);
            string path = Path.Combine(
                AppContext.BaseDirectory, "Assets", "WeatherIcons", "png",
                size.ToString(CultureInfo.InvariantCulture), icon.ToFileName() + ".png");

            if (!File.Exists(path))
                return IntPtr.Zero;

            if (GdipCreateBitmapFromFile(path, out IntPtr bitmap) != 0 || bitmap == IntPtr.Zero)
                return IntPtr.Zero;

            GdipCreateHICONFromBitmap(bitmap, out IntPtr hIcon);
            GdipDisposeImage(bitmap);
            return hIcon;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TrayIconHelper] Asset icon failed, using vector fallback: {ex.Message}");
            return IntPtr.Zero;
        }
    }

    /// <summary>Key understood by <see cref="TrayVectorRenderer"/> for the fallback drawing.</summary>
    private static string FallbackCode(WeatherIcon icon) => icon switch
    {
        WeatherIcon.ClearNight => "🌙",
        WeatherIcon.PartlyCloudyDay or WeatherIcon.PartlyCloudyNight => "⛅",
        WeatherIcon.Cloudy => "☁️",
        WeatherIcon.Fog => "🌫️",
        WeatherIcon.Rain => "🌧️",
        WeatherIcon.Snow => "❄️",
        WeatherIcon.Thunderstorm => "⛈️",
        WeatherIcon.Loading => "🔄",
        WeatherIcon.Error => "⚠️",
        _ => "☀️"
    };

    public static IntPtr CreateEmojiIcon(string emojiOrCode)
    {
        EnsureGdiplus();

        int size = 32;
        int status = GdipCreateBitmapFromScan0(size, size, 0, PixelFormat32bppARGB, IntPtr.Zero, out IntPtr bitmap);
        if (status != 0 || bitmap == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        GdipGetImageGraphicsContext(bitmap, out IntPtr graphics);
        if (graphics == IntPtr.Zero)
        {
            GdipDisposeImage(bitmap);
            return IntPtr.Zero;
        }

        // Clear transparent background (0x00000000)
        GdipGraphicsClear(graphics, 0x00000000);

        // Render crisp weather vector graphics
        TrayVectorRenderer.RenderWeatherIcon(graphics, emojiOrCode);

        // Convert 32bpp ARGB bitmap to Windows HICON handle
        GdipCreateHICONFromBitmap(bitmap, out IntPtr hIcon);

        GdipDeleteGraphics(graphics);
        GdipDisposeImage(bitmap);

        return hIcon;
    }

    public static void Shutdown()
    {
        lock (LockObj)
        {
            if (_gdiplusToken != IntPtr.Zero)
            {
                GdiplusShutdown(_gdiplusToken);
                _gdiplusToken = IntPtr.Zero;
            }
        }
    }
}
