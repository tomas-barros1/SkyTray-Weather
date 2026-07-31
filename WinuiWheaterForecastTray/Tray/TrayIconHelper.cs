using System;
using System.Runtime.InteropServices;
using WinuiWheaterForecastTray.Native;

namespace WinuiWheaterForecastTray.Tray;

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
    private static extern int GdipCreateHICONFromBitmap(IntPtr bitmap, out IntPtr hicon);

    [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipDisposeImage(IntPtr image);

    [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipDeleteGraphics(IntPtr graphics);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyIcon(IntPtr hIcon);

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
