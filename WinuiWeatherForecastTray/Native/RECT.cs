using System.Runtime.InteropServices;

namespace WinuiWeatherForecastTray.Native;

/// <summary>Win32 <c>RECT</c>: screen coordinates in physical pixels.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public readonly int Width => Right - Left;
    public readonly int Height => Bottom - Top;
}
