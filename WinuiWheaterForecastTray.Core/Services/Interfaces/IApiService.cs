using System.Threading;
using System.Threading.Tasks;
using WinuiWheaterForecastTray.DTOs;

namespace WinuiWheaterForecastTray.Services.Interfaces;

public interface IApiService
{
    Task<ApiResponseDTO> GetWeatherDataAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
