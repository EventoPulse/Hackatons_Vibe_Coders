using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventsApp.Models
{
    public enum EventChangeRequestStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
    }

    public class EventChangeRequest
    {
        public EventChangeRequest()
        {
            Status = EventChangeRequestStatus.Pending;
            SubmittedAt = DateTime.UtcNow;
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Event))]
        public int EventId { get; set; }

        public Event Event { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(Organizer))]
        public string OrganizerId { get; set; } = null!;

        public ApplicationUser Organizer { get; set; } = null!;

        [Required]
        public string ChangeJson { get; set; } = null!;

        [Required]
        public EventChangeRequestStatus Status { get; set; }

        [Required]
        public DateTime SubmittedAt { get; set; }

        public DateTime? ReviewedAt { get; set; }

        [ForeignKey(nameof(ReviewedByAdmin))]
        public string? ReviewedByAdminId { get; set; }

        public ApplicationUser? ReviewedByAdmin { get; set; }
    }
}
