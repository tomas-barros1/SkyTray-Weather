using System;
using System.Runtime.InteropServices;

namespace WinuiWheaterForecastTray.Tray;

[StructLayout(LayoutKind.Sequential)]
public struct ICONINFO
{
    public bool fIcon;
    public int xHotspot;
    public int yHotspot;
    public IntPtr hbmMask;
    public IntPtr hbmColor;
}
