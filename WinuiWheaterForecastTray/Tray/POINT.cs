using System.Runtime.InteropServices;

namespace WinuiWheaterForecastTray.Tray;

[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int X;
    public int Y;
}
