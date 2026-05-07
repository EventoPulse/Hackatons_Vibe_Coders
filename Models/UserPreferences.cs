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

        [MaxLength(256)]
        public string? PreferredGenresCsv { get; set; }

        [NotMapped]
        public IReadOnlyList<EventGenre> PreferredGenres
        {
            get
            {
                if (string.IsNullOrWhiteSpace(PreferredGenresCsv))
                {
                    return PreferredGenre.HasValue
                        ? new[] { PreferredGenre.Value }
                        : Array.Empty<EventGenre>();
                }
                var list = new List<EventGenre>();
                foreach (var part in PreferredGenresCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (Enum.TryParse<EventGenre>(part, ignoreCase: true, out var g) && !list.Contains(g))
                    {
                        list.Add(g);
                    }
                }
                return list;
            }
            set
            {
                if (value == null || value.Count == 0)
                {
                    PreferredGenresCsv = null;
                    PreferredGenre = null;
                    return;
                }
                PreferredGenresCsv = string.Join(",", value.Distinct().Select(g => g.ToString()));
                PreferredGenre = value[0];
            }
        }

        [MaxLength(GlobalConstants.Preferences.PreferredCityMaxLength)]
        public string? PreferredCity { get; set; }

        [Range(GlobalConstants.Preferences.MinAgeLower, GlobalConstants.Preferences.MinAgeUpper)]
        public int? MinAge { get; set; }

        public int? MaxDistanceKm { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
