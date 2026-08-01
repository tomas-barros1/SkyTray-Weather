using System;
using System.Diagnostics;

namespace WinuiWheaterForecastTray.Services;

/// <summary>
/// Helper for uniform debug log output across swallow sites and graceful failure paths.
/// </summary>
internal static class DebugLog
{
    public static void Swallowed(Type owner, Exception ex, string? context = null)
    {
        string ctxMsg = string.IsNullOrEmpty(context) ? string.Empty : $" [{context}]";
        Debug.WriteLine($"[{owner.Name}]{ctxMsg} Swallowed {ex.GetType().Name}: {ex.Message}");
    }
}
