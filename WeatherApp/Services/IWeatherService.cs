using MyWeatherApp.Models;

namespace MyWeatherApp.Services
{
    public interface IWeatherService
    {
        Task<WeatherModel?> GetWeatherAsync(string query, string unit);
    }
}
