using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventsApp.Models
{
    public class Follow
    {
        public Follow()
        {
            this.CreatedAt = DateTime.UtcNow;
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Follower))]
        public string FollowerId { get; set; } = null!;

        public ApplicationUser Follower { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(Following))]
        public string FollowingId { get; set; } = null!;

        public ApplicationUser Following { get; set; } = null!;

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
