using System.Runtime.InteropServices;

namespace WinuiWheaterForecastTray.Native;

[StructLayout(LayoutKind.Sequential)]
public struct GdipPointI
{
    public int X;
    public int Y;

    public GdipPointI(int x, int y)
    {
        X = x;
        Y = y;
    }
}
