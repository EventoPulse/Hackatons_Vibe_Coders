using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventsApp.Models
{
    public class Conversation
    {
        public Conversation()
        {
            this.CreatedAt = DateTime.UtcNow;
            this.UpdatedAt = this.CreatedAt;
            this.Messages = new HashSet<Message>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(ParticipantOne))]
        public string ParticipantOneId { get; set; } = null!;

        public ApplicationUser ParticipantOne { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(ParticipantTwo))]
        public string ParticipantTwoId { get; set; } = null!;

        public ApplicationUser ParticipantTwo { get; set; } = null!;

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        // MVP guardrails live in PlatformPermissionService. Add ConversationStatus/IsBlocked
        // columns here when message requests, blocking, and moderation reports get storage.
        public ICollection<Message> Messages { get; set; }
    }
}
