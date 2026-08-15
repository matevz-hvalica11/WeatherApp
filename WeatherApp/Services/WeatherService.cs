<<<<<<< HEAD
﻿using MyWeatherApp.Models;
=======
using MyWeatherApp.Models;
>>>>>>> 8eaf08338debae03da05b4936238aa3c1d788720
using Newtonsoft.Json.Linq;

namespace MyWeatherApp.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<WeatherService> _logger;

        public WeatherService(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<WeatherService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
        }

        public async Task<WeatherModel?> GetWeatherAsync(string query, string unit)
        {
            string? apiKey = _config["WeatherApi:Key"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError("WeatherAPI key is missing from configuration.");
                return null;
            }

            string encodedQuery = Uri.EscapeDataString(query);
            string url = $"https://api.weatherapi.com/v1/forecast.json?key={apiKey}&q={encodedQuery}&days=3&lang=sl&aqi=yes&alerts=yes";

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("WeatherAPI returned {StatusCode}", response.StatusCode);
                    return null;
                }

                string json = await response.Content.ReadAsStringAsync();

                JObject data;
                try
                {
                    data = JObject.Parse(json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse WeatherAPI response.");
                    return null;
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
                    Latitude = (double?)data["location"]?["lat"] ?? 0,
                    Longitude = (double?)data["location"]?["lon"] ?? 0,
                    Forecasts = new(),
                    Hourly = new()
                };

                model.CurrentPrecipitation = (double?)data["current"]?[unit == "F" ? "precip_in" : "precip_mm"] ?? 0;

                // Dew Point
                double tempC = unit == "F"
                    ? ((model.CurrentTemperature - 32.0) * 5.0 / 9.0)
                    : model.CurrentTemperature;

                model.DewPointTemperature = ComputeDewPointC(tempC, model.CurrentHumidity);
                if (unit == "F")
                    model.DewPointTemperature = (model.DewPointTemperature * 9.0 / 5.0) + 32.0;

                // Air Quality
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

                // Hourly forecast
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

                // Daily forecasts
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

                // Weather alerts
                var alertsArray = data["alerts"]?["alert"];
                if (alertsArray != null)
                {
                    foreach (var alert in alertsArray)
                    {
                        model.Alerts.Add(new WeatherModel.WeatherAlert
                        {
                            Headline = alert["headline"]?.ToString() ?? "",
                            Severity = alert["severity"]?.ToString() ?? "",
                            Event = alert["event"]?.ToString() ?? "",
                            Description = alert["desc"]?.ToString() ?? "",
                            Effective = alert["effective"]?.ToString() ?? "",
                            Expires = alert["expires"]?.ToString() ?? ""
                        });
                    }
                }

                return model;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to WeatherAPI failed.");
                return null;
            }
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
                1 => "Good",
                2 => "Moderate",
                3 => "Unhealthy for Sensitive Groups",
                4 => "Unhealthy",
                5 => "Very Unhealthy",
                6 => "Hazardous",
                _ => "Unknown"
            };
        }
    }
}
