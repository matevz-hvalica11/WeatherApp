using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyWeatherApp.Models;

namespace MyWeatherApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<SavedLocation> SavedLocations { get; set; }
        public DbSet<SearchHistory> SearchHistories { get; set; }
    }
}
