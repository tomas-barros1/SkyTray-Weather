using System;

namespace WinuiWeatherForecastTray.Core.Services;

public static class UpdateLaunchCommandBuilder
{
    public static string Create(string installerPath, string applicationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installerPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationPath);
        ValidatePath(installerPath, nameof(installerPath));
        ValidatePath(applicationPath, nameof(applicationPath));

        return $"/c \"\"{installerPath}\" /SILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS && start \"\" \"{applicationPath}\"\"";
    }

    private static void ValidatePath(string path, string parameterName)
    {
        if (path.Contains('"') || path.Contains('%'))
            throw new ArgumentException("Update paths cannot contain quotes or percent signs.", parameterName);
    }
}
