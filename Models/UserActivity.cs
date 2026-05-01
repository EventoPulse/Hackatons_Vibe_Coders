using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventsApp.Common;

namespace EventsApp.Models
{
    public class UserActivity
    {
        public UserActivity()
        {
            this.CreatedAt = DateTime.UtcNow;
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(User))]
        public string UserId { get; set; } = null!;

        public ApplicationUser User { get; set; } = null!;

        [Required]
        public UserActivityType ActivityType { get; set; }

        public int? EventId { get; set; }

        public Event? Event { get; set; }

        public int? PostId { get; set; }

        public Post? Post { get; set; }

        public string? TargetUserId { get; set; }

        public ApplicationUser? TargetUser { get; set; }

        [MaxLength(GlobalConstants.Social.ActivityValueMaxLength)]
        public string? Value { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
