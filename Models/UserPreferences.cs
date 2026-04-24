using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventsApp.Common;

namespace EventsApp.Models
{
    public class UserPreferences
    {
        public UserPreferences()
        {
            this.CreatedAt = DateTime.UtcNow;
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(User))]
        public string UserId { get; set; } = null!;

        public ApplicationUser User { get; set; } = null!;

        public EventGenre? PreferredGenre { get; set; }

        [MaxLength(GlobalConstants.Preferences.PreferredCityMaxLength)]
        public string? PreferredCity { get; set; }

        [Range(GlobalConstants.Preferences.MinAgeLower, GlobalConstants.Preferences.MinAgeUpper)]
        public int? MinAge { get; set; }

        public int? MaxDistanceKm { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
