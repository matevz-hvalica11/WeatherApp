using Microsoft.AspNetCore.Identity;
using MyWeatherApp.Models;

namespace MyWeatherApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<SavedLocation> SavedLocations { get; set; } = new List<SavedLocation>();
        public ICollection<SearchHistory> SearchHistories { get; set; } = new List<SearchHistory>();
    }
}
