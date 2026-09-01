using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MyWeatherApp.Data;
using MyWeatherApp.Models;
using MyWeatherApp.Services;
using System.Globalization;

namespace MyWeatherApp.Controllers
{
    public class WeatherController : Controller
    {
        private readonly IWeatherService _weatherService;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public WeatherController(IWeatherService weatherService, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
        {
            _weatherService = weatherService;
            _dbContext = dbContext;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string city = "", double? lat = null, double? lon = null, string unit = "C")
        {
            string query;

            if (lat.HasValue && lon.HasValue)
                query = $"{lat.Value.ToString(CultureInfo.InvariantCulture)},{lon.Value.ToString(CultureInfo.InvariantCulture)}";
            else if (!string.IsNullOrWhiteSpace(city))
                query = city;
            else
                query = "Ljubljana";

            var model = await _weatherService.GetWeatherAsync(query, unit);

            if (model == null)
            {
                if (lat.HasValue && lon.HasValue)
                    return Content("NO_WEATHER_DATA");
                return Content("Could not retrieve weather data. Please try again.");
            }

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                ViewBag.IsSaved = await _dbContext.SavedLocations
                    .AnyAsync(s => s.UserId == userId && s.CityName == model.CityName);
            }
            else
            {
                ViewBag.IsSaved = false;
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SaveLocation(string cityName, double lat, double lon)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            bool exists = await _dbContext.SavedLocations
                .AnyAsync(s => s.UserId == userId && s.CityName == cityName);

            if (!exists)
            {
                _dbContext.SavedLocations.Add(new SavedLocation
                {
                    CityName = cityName,
                    Latitude = lat,
                    Longitude = lon,
                    UserId = userId
                });
                await _dbContext.SaveChangesAsync();
            }

            return RedirectToAction("Index", new { city = cityName });
        }

        [Authorize]
        public async Task<IActionResult> SavedLocations()
        {
            var userId = _userManager.GetUserId(User);
            var locations = await _dbContext.SavedLocations
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.SavedAt)
                .ToListAsync();

            return View(locations);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteSavedLocation(int id)
        {
            var userId = _userManager.GetUserId(User);
            var location = await _dbContext.SavedLocations
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (location != null)
            {
                _dbContext.SavedLocations.Remove(location);
                await _dbContext.SaveChangesAsync();
            }

            return RedirectToAction("SavedLocations");
        }
    }
}