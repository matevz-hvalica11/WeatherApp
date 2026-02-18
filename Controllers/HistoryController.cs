using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using MyWeatherApp_Deployed.Models;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace MyWeatherApp_Deployed.Controllers
{
    public class HistoryController : Controller
    {
        private readonly IConfiguration _config;

        public HistoryController(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IActionResult> Index(string city = "", string date = "")
        {

            Console.WriteLine($"[HISTORY DEBUG] city='{city}', date='{date}'");

            if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(date))
                return View("HistorySearch");


            ViewBag.City = city;
            ViewBag.Date = date;


            // Build the WeatherAPI history endpoint
            string apiKey = _config["WeatherApi:Key"];


            string url = $"https://api.weatherapi.com/v1/history.json?key={apiKey}&q={city}&dt={date}";
            ViewBag.ApiUrl = url;


            using var client = new HttpClient();
            var response = await client.GetAsync(url);


            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = $"API returned status {response.StatusCode}";
                return View("HistoryResult");
            }

            string json = await response.Content.ReadAsStringAsync();
            ViewBag.RawJson = json;

            var data = JObject.Parse(json);
            var hourArray = data["forecast"]?["forecastday"]?[0]?["hour"];

            var day = data["forecast"]?["forecastday"]?[0]["day"];

            if (day == null)
            {
                ViewBag.Error = "Could not read historical data.";
                return View("HistoryResult");
            }

            // Build the model
            var model = new WeatherHistoryModel
            {
                City = city,
                Date = DateTime.Parse(date),

                AvgTempC = (double?)day["avgtemp_c"] ?? 0,
                MaxTempC = (double?)day["maxtemp_c"] ?? 0,
                MinTempC = (double?)day["mintemp_c"] ?? 0,
                AvgHumidity = (double?)day["avghumidity"] ?? 0,
                TotalPrecipMm = (double?)day["totalprecip_mm"] ?? 0,
            };


            // Hourly History
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


            // Send model to results view
            return View("HistoryResult", model);
        }
    }
}
