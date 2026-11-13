namespace WinuiWeatherForecastTray.Models;

/// <summary>
/// The weather icons shipped in <c>Assets/WeatherIcons</c>. <see cref="Unknown"/> (0) is what an old cached
/// forecast without icon data deserializes to; the UI shows no icon for it.
/// </summary>
public enum WeatherIcon
{
    Unknown = 0,
    ClearDay,
    ClearNight,
    PartlyCloudyDay,
    PartlyCloudyNight,
    Cloudy,
    Fog,
    Rain,
    Snow,
    Thunderstorm,
    Loading,
    Error
}

public static class WeatherIconExtensions
{
    /// <summary>File name (without extension) of the icon's SVG/PNG assets, e.g. <c>partly-cloudy-day</c>.</summary>
    public static string ToFileName(this WeatherIcon icon) => icon switch
    {
        WeatherIcon.ClearDay => "clear-day",
        WeatherIcon.ClearNight => "clear-night",
        WeatherIcon.PartlyCloudyDay => "partly-cloudy-day",
        WeatherIcon.PartlyCloudyNight => "partly-cloudy-night",
        WeatherIcon.Cloudy => "cloudy",
        WeatherIcon.Fog => "fog",
        WeatherIcon.Rain => "rain",
        WeatherIcon.Snow => "snow",
        WeatherIcon.Thunderstorm => "thunderstorm",
        WeatherIcon.Loading => "loading",
        WeatherIcon.Error => "error",
        _ => string.Empty
    };
}
