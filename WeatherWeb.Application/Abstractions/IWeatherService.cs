using System.Threading;
using System.Threading.Tasks;
using WeatherWeb.Application.Features.Weather.DTOs;

namespace WeatherWeb.Application.Abstractions;

public interface IWeatherService
{
    Task<WeatherViewModel?> GetCurrentAsync(string location, CancellationToken ct = default);
    Task<WeatherViewModel?> GetCurrentByCoordinatesAsync(double latitude, double longitude, CancellationToken ct = default);
}
