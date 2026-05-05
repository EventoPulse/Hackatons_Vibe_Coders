using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventsApp.Models
{
    public class OrganizerValidatorAssignment
    {
        public OrganizerValidatorAssignment()
        {
            this.CreatedAt = DateTime.UtcNow;
            this.IsActive = true;
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Organizer))]
        public string OrganizerId { get; set; } = null!;

        public ApplicationUser Organizer { get; set; } = null!;

        public int? OrganizerProfileId { get; set; }

        public OrganizerProfile? OrganizerProfile { get; set; }

        [Required]
        [ForeignKey(nameof(ValidatorUser))]
        public string ValidatorUserId { get; set; } = null!;

        public ApplicationUser ValidatorUser { get; set; } = null!;

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
