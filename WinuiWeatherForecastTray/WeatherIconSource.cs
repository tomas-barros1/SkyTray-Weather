using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using WinuiWeatherForecastTray.Models;

namespace WinuiWeatherForecastTray
{
    /// <summary>
    /// Provides the weather icons (SVG files in <c>Assets/WeatherIcons/svg</c>) as XAML image sources.
    /// The files are read from disk and handed to <see cref="SvgImageSource"/> as a stream, which works for
    /// this unpackaged app without relying on <c>ms-appx:</c> URI resolution. Sources are created once and cached.
    /// Must be called on the UI thread (it creates XAML objects); also used from <c>x:Bind</c> in the hourly list.
    /// </summary>
    internal static class WeatherIconSource
    {
        private static readonly Dictionary<WeatherIcon, SvgImageSource> Cache = new();

        private static string SvgDirectory =>
            Path.Combine(AppContext.BaseDirectory, "Assets", "WeatherIcons", "svg");

        /// <summary>Returns the image source for <paramref name="icon"/>, or <c>null</c> if it has no asset.</summary>
        public static ImageSource? Get(WeatherIcon icon)
        {
            if (icon == WeatherIcon.Unknown)
                return null;

            if (Cache.TryGetValue(icon, out SvgImageSource? cached))
                return cached;

            string path = Path.Combine(SvgDirectory, icon.ToFileName() + ".svg");
            if (!File.Exists(path))
            {
                System.Diagnostics.Debug.WriteLine($"[WeatherIconSource] Missing asset: {path}");
                return null;
            }

            var source = new SvgImageSource();
            Cache[icon] = source;
            _ = LoadAsync(source, path); // the Image shows the icon as soon as the load completes
            return source;
        }

        private static async Task LoadAsync(SvgImageSource source, string path)
        {
            try
            {
                byte[] bytes = await File.ReadAllBytesAsync(path);

                using var stream = new InMemoryRandomAccessStream();
                using (var writer = new DataWriter(stream))
                {
                    writer.WriteBytes(bytes);
                    await writer.StoreAsync();
                    writer.DetachStream();
                }

                stream.Seek(0);
                SvgImageSourceLoadStatus status = await source.SetSourceAsync(stream);
                if (status != SvgImageSourceLoadStatus.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"[WeatherIconSource] {path} failed to load: {status}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WeatherIconSource] {path} failed to load: {ex.Message}");
            }
        }
    }
}
