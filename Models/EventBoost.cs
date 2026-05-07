using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventsApp.Models
{
    public class EventBoost
    {
        public EventBoost()
        {
            CreatedAt = DateTime.UtcNow;
            CreditsSpent = 1;
        }

        [Key]
        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }

        public Event Event { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(Organizer))]
        public string OrganizerId { get; set; } = null!;

        public ApplicationUser Organizer { get; set; } = null!;

        [Required]
        public int CreditsSpent { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
