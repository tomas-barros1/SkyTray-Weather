using System.Threading;
using System.Threading.Tasks;
using WinuiWeatherForecastTray.DTOs;

namespace WinuiWeatherForecastTray.Services.Interfaces;

public interface IApiService
{
    Task<ApiResponseDTO> GetWeatherDataAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
