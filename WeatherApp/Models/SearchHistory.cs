using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyWeatherApp.Models
{
    public class SearchHistory
    {
        public int Id { get; set; }

        [Required]
        public string CityName { get; set; } = string.Empty;

        public DateTime SearchedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null;
    }
}
