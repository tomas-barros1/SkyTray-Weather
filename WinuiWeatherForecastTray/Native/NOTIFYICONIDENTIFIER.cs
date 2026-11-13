using System;
using System.Runtime.InteropServices;

namespace WinuiWeatherForecastTray.Native;

/// <summary>Identifies a tray icon for <c>Shell_NotifyIconGetRect</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NOTIFYICONIDENTIFIER
{
    public uint cbSize;
    public IntPtr hWnd;
    public uint uID;
    public Guid guidItem;
}
