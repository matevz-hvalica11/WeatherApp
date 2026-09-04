using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using MyWeatherApp.Models;

namespace MyWeatherApp.Controllers
{
    public class HistoryController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HistoryController> _logger;

        public HistoryController(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<HistoryController> logger)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string city = "", string date = "")
        {
            // No inputs - show the search form
            if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(date))
                return View("HistorySearch");

            // Validate date
            if (!DateTime.TryParse(date, out DateTime parsedDate))
            {
                ViewBag.Error = "Invalid date format. Please use YYYY-MM-DD.";
                return View("HistoryResult");
            }

            ViewBag.City = city;
            ViewBag.Date = date;

            string apiKey = _config["WeatherApi:Key"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ViewBag.Error = "Weather API key is missing.";
                return View("HistoryResult");
            }

            string encodedCity = Uri.EscapeDataString(city);
            string url = $"https://api.weatherapi.com/v1/history.json?key={apiKey}&q={encodedCity}&dt={date}";

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("WeatherAPI history request failed: {StatusCode}", response.StatusCode);
                    ViewBag.Error = "Historical weather is only available for the last 24 hours on the free WeatherAPI plan. Please enter yesterday's or today's date.";
                    return View("HistoryResult");
                }

                string json = await response.Content.ReadAsStringAsync();

                JObject data;
                try
                {
                    data = JObject.Parse(json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse WeatherAPI history response.");
                    ViewBag.Error = "Weather API returned invalid data.";
                    return View("HistoryResult");
                }

                var forecastDay = data["forecast"]?["forecastday"]?[0];
                var day = forecastDay["day"];
                var hourArray = forecastDay?["hour"];

                if (day == null)
                {
                    ViewBag.Error = "Could not read historical data.";
                    return View("HistoryResult");
                }

                // Build the model
                var model = new WeatherHistoryModel
                {
                    City = city,
                    Date = parsedDate,
                    AvgTempC = (double?)day["avgtemp_c"] ?? 0,
                    MaxTempC = (double?)day["maxtemp_c"] ?? 0,
                    MinTempC = (double?)day["mintemp_c"] ?? 0,
                    AvgHumidity = (double?)day["avghumidity"] ?? 0,
                    TotalPrecipMm = (double?)day["totalprecip_mm"] ?? 0,
                };

                // Hourly history
                if (hourArray != null)
                {
                    foreach (var hour in hourArray)
                    {
                        model.Hourly.Add(new WeatherHistoryModel.HourlyHistory
                        {
                            Time = hour["time"]?.ToString() ?? "",
                            TempC = (double?)hour["temp_c"] ?? 0,
                            Humidity = (double?)hour["humidity"] ?? 0,
                            PrecipMm = (double?)hour["precip_mm"] ?? 0
                        });
                    }
                }

                return View("HistoryResult", model);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to WeatherAPI failed.");
                ViewBag.Error = "Could not reach the weather service. Please try again.";
                return View("HistoryResult");
            }
        }
    }
}