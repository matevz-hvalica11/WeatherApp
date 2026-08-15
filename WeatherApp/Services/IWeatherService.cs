<<<<<<< HEAD
﻿using MyWeatherApp.Models;
=======
using MyWeatherApp.Models;
>>>>>>> 8eaf08338debae03da05b4936238aa3c1d788720

namespace MyWeatherApp.Services
{
    public interface IWeatherService
    {
        Task<WeatherModel?> GetWeatherAsync(string query, string unit);
    }
}
