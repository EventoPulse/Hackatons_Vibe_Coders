using System.ComponentModel.DataAnnotations;
using EventsApp.Common;
using EventsApp.Models;

namespace EventsApp.ViewModels.Preferences
{
    public class PreferencesViewModel
    {
        [Display(Name = "Preferred genre")]
        public EventGenre? PreferredGenre { get; set; }

        [StringLength(GlobalConstants.Preferences.PreferredCityMaxLength)]
        [Display(Name = "Preferred city")]
        public string? PreferredCity { get; set; }

        [Range(GlobalConstants.Preferences.MinAgeLower, GlobalConstants.Preferences.MinAgeUpper)]
        [Display(Name = "Minimum age")]
        public int? MinAge { get; set; }

        [Range(0, 10000)]
        [Display(Name = "Max distance (km)")]
        public int? MaxDistanceKm { get; set; }
    }
}
