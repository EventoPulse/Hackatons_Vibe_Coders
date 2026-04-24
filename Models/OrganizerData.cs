using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventsApp.Common;

namespace EventsApp.Models
{
    public class OrganizerData
    {
        public OrganizerData()
        {
            this.CreatedAt = DateTime.UtcNow;
            this.Approved = false;
        }

        [Key]
        [ForeignKey(nameof(Organizer))]
        public string OrganizerId { get; set; } = null!;

        public ApplicationUser Organizer { get; set; } = null!;

        [Required]
        [MinLength(GlobalConstants.Organizer.OrganizationNameMinLength)]
        [MaxLength(GlobalConstants.Organizer.OrganizationNameMaxLength)]
        public string OrganizationName { get; set; } = null!;

        [MaxLength(GlobalConstants.Organizer.DescriptionMaxLength)]
        public string? Description { get; set; }

        [MaxLength(GlobalConstants.Organizer.PhoneNumberMaxLength)]
        public string? PhoneNumber { get; set; }

        [MaxLength(GlobalConstants.Organizer.WebsiteMaxLength)]
        public string? Website { get; set; }

        [MaxLength(GlobalConstants.Organizer.CompanyNumberMaxLength)]
        public string? CompanyNumber { get; set; }

        [Required]
        public bool Approved { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
