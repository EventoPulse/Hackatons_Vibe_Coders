using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventsApp.Common;

namespace EventsApp.Models
{
    public class Message
    {
        public Message()
        {
            this.CreatedAt = DateTime.UtcNow;
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Conversation))]
        public int ConversationId { get; set; }

        public Conversation Conversation { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(Sender))]
        public string SenderId { get; set; } = null!;

        public ApplicationUser Sender { get; set; } = null!;

        [Required]
        [MinLength(GlobalConstants.Social.MessageContentMinLength)]
        [MaxLength(GlobalConstants.Social.MessageContentMaxLength)]
        public string Content { get; set; } = null!;

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? SeenAt { get; set; }
    }
}
