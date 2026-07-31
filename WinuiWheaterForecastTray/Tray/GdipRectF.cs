using System.Runtime.InteropServices;

namespace WinuiWheaterForecastTray.Tray;

[StructLayout(LayoutKind.Sequential)]
public struct GdipRectF
{
    public float X;
    public float Y;
    public float Width;
    public float Height;

    public GdipRectF(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}
