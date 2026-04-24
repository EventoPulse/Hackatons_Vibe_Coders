using System.ComponentModel.DataAnnotations;
using EventsApp.Common;

namespace EventsApp.ViewModels.Organizer
{
    public class OrganizerProfileViewModel
    {
        [Required]
        [StringLength(GlobalConstants.Organizer.OrganizationNameMaxLength, MinimumLength = GlobalConstants.Organizer.OrganizationNameMinLength)]
        [Display(Name = "Organization name")]
        public string OrganizationName { get; set; } = null!;

        [StringLength(GlobalConstants.Organizer.DescriptionMaxLength)]
        public string? Description { get; set; }

        [StringLength(GlobalConstants.Organizer.PhoneNumberMaxLength)]
        [Phone]
        [Display(Name = "Phone")]
        public string? PhoneNumber { get; set; }

        [StringLength(GlobalConstants.Organizer.WebsiteMaxLength)]
        [Url]
        public string? Website { get; set; }

        [StringLength(GlobalConstants.Organizer.CompanyNumberMaxLength)]
        [Display(Name = "Company number")]
        public string? CompanyNumber { get; set; }

        public bool Approved { get; set; }
    }
}
