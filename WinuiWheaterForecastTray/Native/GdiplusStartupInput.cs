using System;
using System.Runtime.InteropServices;

namespace WinuiWheaterForecastTray.Native;

[StructLayout(LayoutKind.Sequential)]
public struct GdiplusStartupInput
{
    public uint GdiplusVersion;
    public IntPtr DebugEventCallback;
    public bool SuppressBackgroundThread;
    public bool SuppressExternalCodecs;

    public static GdiplusStartupInput Default
    {
        get
        {
            return new GdiplusStartupInput
            {
                GdiplusVersion = 1,
                DebugEventCallback = IntPtr.Zero,
                SuppressBackgroundThread = false,
                SuppressExternalCodecs = false
            };
        }
    }
}
