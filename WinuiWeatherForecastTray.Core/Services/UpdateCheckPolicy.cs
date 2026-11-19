using System;

namespace WinuiWeatherForecastTray.Core.Services;

public static class UpdateCheckPolicy
{
    public static DateTime GetNextAutomaticCheckUtc(DateTime nowUtc, DateTime? lastCheckUtc, bool isStartup)
    {
        DateTime candidate = nowUtc.Add(isStartup
            ? AppUpdateConstants.InitialUpdateCheckDelay
            : AppUpdateConstants.AutomaticUpdateCheckInterval);
        if (lastCheckUtc is not DateTime lastCheck)
            return candidate;

        DateTime minimumAllowed = DateTime.SpecifyKind(lastCheck, DateTimeKind.Utc)
            .Add(AppUpdateConstants.MinimumUpdateCheckInterval);
        return minimumAllowed > candidate ? minimumAllowed : candidate;
    }

    public static bool CanCheckNow(DateTime nowUtc, DateTime? lastCheckUtc)
    {
        return lastCheckUtc is null ||
            nowUtc - DateTime.SpecifyKind(lastCheckUtc.Value, DateTimeKind.Utc) >= AppUpdateConstants.MinimumUpdateCheckInterval;
    }
}
