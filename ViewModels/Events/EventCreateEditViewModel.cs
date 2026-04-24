using System.ComponentModel.DataAnnotations;
using EventsApp.Common;
using EventsApp.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventsApp.ViewModels.Events
{
    public class EventCreateEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(GlobalConstants.Event.TitleMaxLength, MinimumLength = GlobalConstants.Event.TitleMinLength)]
        public string Title { get; set; } = null!;

        [StringLength(GlobalConstants.Event.DescriptionMaxLength)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Venue")]
        public int VenueId { get; set; }

        [Required]
        [Display(Name = "Start time")]
        [DataType(DataType.DateTime)]
        public DateTime StartTime { get; set; } = DateTime.UtcNow.AddDays(1);

        [Required]
        [Display(Name = "End time")]
        [DataType(DataType.DateTime)]
        public DateTime EndTime { get; set; } = DateTime.UtcNow.AddDays(1).AddHours(2);

        [Required]
        public EventGenre Genre { get; set; }

        [Url]
        [StringLength(GlobalConstants.Event.ImageUrlMaxLength)]
        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Approved")]
        public bool IsApproved { get; set; }

        public bool CanEditApproval { get; set; }

        public IEnumerable<SelectListItem> Venues { get; set; } = Array.Empty<SelectListItem>();
    }
}
