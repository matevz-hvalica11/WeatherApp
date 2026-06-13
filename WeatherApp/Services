using Microsoft.AspNetCore.Mvc;
using MyWeatherApp.Models;
using MyWeatherApp.Services;
using System.Globalization;

namespace MyWeatherApp.Controllers
{
    public class WeatherController : Controller
    {
        private readonly IWeatherService _weatherService;

        public WeatherController(IWeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        public async Task<IActionResult> Index(string city = "", double? lat = null, double? lon = null, string unit = "C")
        {
            string query;

            if (lat.HasValue && lon.HasValue)
            {
                query = $"{lat.Value.ToString(CultureInfo.InvariantCulture)},{lon.Value.ToString(CultureInfo.InvariantCulture)}";
            }
            else if (!string.IsNullOrWhiteSpace(city))
            {
                query = city;
            }
            else
            {
                return View(new WeatherModel { CityName = "", TempUnit = unit });
            }

            var model = await _weatherService.GetWeatherAsync(query, unit);

            if (model == null)
            {
                return Content("Could not retrieve weather data. Please try again.");
            }

            return View(model);
        }
    }
}
