using Microsoft.AspNetCore.Mvc;
using MyWeatherApp_Deployed.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace MyWeatherApp_Deployed.Controllers
{
    public class WeatherController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly IConfiguration _config;

        public WeatherController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public async Task<IActionResult> Index(string city = "", double? lat = null, double? lon = null, string unit = "C")
        {

            string apiKey = _config["WeatherApi:Key"];

            if (string.IsNullOrWhiteSpace(apiKey))
                return Content("Weather API key missing in configuration.");

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
                return View(new WeatherModel
                {
                    CityName = "",
                    TempUnit = unit
                });
            }

                string url = $"https://api.weatherapi.com/v1/forecast.json?key={apiKey}&q={query}&days=3&lang=sl&aqi=yes";

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return Content($"status: {(int)response.StatusCode}, reason: {response.ReasonPhrase}");


            string json = await response.Content.ReadAsStringAsync();
            JObject data;
            try
            {
                data = JObject.Parse(json);
            }
            catch
            {
                return Content("Weather API returned invalid data.");
            }

            var model = new WeatherModel
            {
                CityName = data["location"]?["name"]?.ToString() ?? "Unknown",
                CurrentCondition = data["current"]?["condition"]?["text"]?.ToString() ?? "",
                CurrentTemperature = (double?)data["current"]?[unit == "F" ? "temp_f" : "temp_c"] ?? 0,
                FeelsLikeTemperature = (double?)data["current"]?[unit == "F" ? "feelslike_f" : "feelslike_c"] ?? 0,
                CurrentHumidity = (int?)data["current"]?["humidity"] ?? 0,
                CurrentWindSpeed = unit == "F"
                    ? ((double?)data["current"]?["wind_kph"] ?? 0) * 0.621371
                    : (double?)data["current"]?["wind_kph"] ?? 0,
                UVIndex = (double?)data["current"]?["uv"] ?? 0,
                CurrentIconUrl = "https:" + data["current"]?["condition"]?["icon"]?.ToString(),
                TempUnit = unit == "F" ? "F" : "C",
                Forecasts = new(),
                Hourly = new()
            };

            model.CurrentPrecipitation = (double?)data["current"]?[unit == "F" ? "precip_in" : "precip_mm"] ?? 0;

            double tempC = unit == "F"
                ? ((model.CurrentTemperature - 32.0) * 5.0 / 9.0)
                : model.CurrentTemperature;

            // Air Quality
            model.DewPointTemperature = ComputeDewPointC(tempC, model.CurrentHumidity);
            if (unit == "F")
            {
                model.DewPointTemperature = (model.DewPointTemperature * 9.0 / 5.0) + 32.0;
            }

            var aq = data["current"]?["air_quality"];
            if (aq != null)
            {
                model.AirQuality = new WeatherModel.AirQualityData
                {
                    CO = (double?)aq["co"] ?? 0,
                    NO2 = (double?)aq["no2"] ?? 0,
                    O3 = (double?)aq["o3"] ?? 0,
                    SO2 = (double?)aq["so2"] ?? 0,
                    PM2_5 = (double?)aq["pm2_5"] ?? 0,
                    PM10 = (double?)aq["pm10"] ?? 0,
                    UsEpaIndex = (int?)aq["us-epa-index"] ?? 0,
                    GbDefraIndex = (int?)aq["gb-defra-index"] ?? 0
                };
                model.AirQuality.Category = EpaCategory(model.AirQuality.UsEpaIndex);
            }

            // Hourly Forecast
            var hourlyArray = data["forecast"]?["forecastday"]?[0]?["hour"];
            if (hourlyArray != null)
            {
                foreach (var hour in hourlyArray)
                {
                    model.Hourly.Add(new WeatherModel.HourlyForecast
                    {
                        Time = hour["time"]?.ToString() ?? "",
                        Temperature = (double?)hour[unit == "F" ? "temp_f" : "temp_c"] ?? 0,
                        Condition = hour["condition"]?["text"]?.ToString() ?? "",
                        Precipitation = (double?)hour[unit == "F" ? "precip_in" : "precip_mm"] ?? 0,
                        ChanceOfRain = (int?)hour["chance_of_rain"] ?? 0,
                        ChanceOfSnow = (int?)hour["chance_of_snow"] ?? 0
                    });
                }
            }

            // Daily Forecasts
            foreach (var day in data["forecast"]?["forecastday"] ?? new JArray())
            {
                model.Forecasts.Add(new WeatherModel.DailyForecast
                {
                    Date = day["date"]?.ToString() ?? "",
                    Description = day["day"]?["condition"]?["text"]?.ToString() ?? "",
                    MaxTemp = (double?)day["day"]?[unit == "F" ? "maxtemp_f" : "maxtemp_c"] ?? 0,
                    MinTemp = (double?)day["day"]?[unit == "F" ? "mintemp_f" : "mintemp_c"] ?? 0,
                    TotalPrecipitation = (double?)day["day"]?[unit == "F" ? "totalprecip_in" : "totalprecip_mm"] ?? 0
                });
            }

            // Leaflet.js
            model.Latitude = (double?)data["location"]?["lat"] ?? 0;
            model.Longitude = (double?)data["location"]?["lon"] ?? 0;


            return View(model);
        }


        private static double ComputeDewPointC(double tempC, int relativeHumidity)
        {
            const double a = 17.27;
            const double b = 237.7;
            double rh = Math.Max(1.0, Math.Min(100.0, relativeHumidity));
            double alpha = ((a * tempC) / (b + tempC)) + Math.Log(rh / 100.0);
            return (b * alpha) / (a - alpha);
        }

        private static string EpaCategory(int index)
        {
            return index switch
            {
                0 => "Good",
                1 => "Moderate",
                2 => "Unhealthy for Sensitive Groups",
                3 => "Unhealthy",
                4 => "Very Unhealthy",
                5 => "Hazardous",
                _ => "Unknown"
            };
        }
    }
}
