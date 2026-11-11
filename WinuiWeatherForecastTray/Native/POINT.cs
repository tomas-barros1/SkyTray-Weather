using System.Runtime.InteropServices;

namespace WinuiWeatherForecastTray.Native;

[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int X;
    public int Y;
}
