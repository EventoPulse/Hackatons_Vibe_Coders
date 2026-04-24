using System.ComponentModel.DataAnnotations;
using EventsApp.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventsApp.ViewModels.Venues
{
    public class VenueCreateEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(GlobalConstants.Venue.NameMaxLength, MinimumLength = GlobalConstants.Venue.NameMinLength)]
        public string Name { get; set; } = null!;

        [StringLength(GlobalConstants.Venue.DescriptionMaxLength)]
        public string? Description { get; set; }

        [Required]
        [StringLength(GlobalConstants.Venue.AddressMaxLength)]
        public string Address { get; set; } = null!;

        [Required]
        [StringLength(GlobalConstants.Venue.CityMaxLength)]
        public string City { get; set; } = null!;

        [Url]
        [StringLength(GlobalConstants.Venue.ImageUrlMaxLength)]
        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Organizer")]
        public string? OrganizerId { get; set; }

        public bool CanPickOrganizer { get; set; }

        public IEnumerable<SelectListItem> Organizers { get; set; } = Array.Empty<SelectListItem>();
    }
}
