using System;
using System.Runtime.InteropServices;

namespace WinuiWheaterForecastTray.Native;

[StructLayout(LayoutKind.Sequential)]
public struct ICONINFO
{
    public bool fIcon;
    public int xHotspot;
    public int yHotspot;
    public IntPtr hbmMask;
    public IntPtr hbmColor;
}
