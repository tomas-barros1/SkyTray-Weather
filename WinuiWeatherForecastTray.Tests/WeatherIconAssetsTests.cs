using FluentAssertions;
using WinuiWeatherForecastTray.Models;
using Xunit;

namespace WinuiWeatherForecastTray.Tests;

/// <summary>
/// Guards the contract between <see cref="WeatherIcon.ToFileName"/> and the files shipped in
/// <c>WinuiWeatherForecastTray/Assets/WeatherIcons</c>: a renamed enum value or a missing file would
/// otherwise only show up as a blank icon at runtime.
/// </summary>
public class WeatherIconAssetsTests
{
    // Must match TrayIconHelper.AssetSizes (16/20/24/32 px = small-icon size at 100/125/150/200% scale).
    private static readonly int[] TrayPngSizes = { 16, 20, 24, 32 };

    private static string FindIconsDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, "WinuiWeatherForecastTray", "Assets", "WeatherIcons");
            if (Directory.Exists(candidate))
                return candidate;

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate WinuiWeatherForecastTray/Assets/WeatherIcons above the test output folder.");
    }

    public static IEnumerable<object[]> RealIcons() =>
        Enum.GetValues<WeatherIcon>().Where(i => i != WeatherIcon.Unknown).Select(i => new object[] { i });

    [Theory]
    [MemberData(nameof(RealIcons))]
    public void EveryIcon_HasAnSvgFile(WeatherIcon icon)
    {
        string path = Path.Combine(FindIconsDirectory(), "svg", icon.ToFileName() + ".svg");

        File.Exists(path).Should().BeTrue($"{icon} needs {path}");
        new FileInfo(path).Length.Should().BeGreaterThan(0);
    }

    [Theory]
    [MemberData(nameof(RealIcons))]
    public void EveryIcon_HasATrayPngForEverySize(WeatherIcon icon)
    {
        foreach (int size in TrayPngSizes)
        {
            string path = Path.Combine(FindIconsDirectory(), "png", size.ToString(), icon.ToFileName() + ".png");

            File.Exists(path).Should().BeTrue($"{icon} needs a {size}px PNG at {path}");
        }
    }
}
