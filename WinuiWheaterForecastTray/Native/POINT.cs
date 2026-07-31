using System.Runtime.InteropServices;

namespace WinuiWheaterForecastTray.Native;

[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int X;
    public int Y;
}
