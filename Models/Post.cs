using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventsApp.Common;

namespace EventsApp.Models
{
    public class Post
    {
        public Post()
        {
            this.CreatedAt = DateTime.UtcNow;

            this.Images = new HashSet<PostImage>();
            this.Comments = new HashSet<PostComment>();
            this.Likes = new HashSet<PostLike>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Organizer))]
        public string OrganizerId { get; set; } = null!;

        public ApplicationUser Organizer { get; set; } = null!;

        [ForeignKey(nameof(Event))]
        public int? EventId { get; set; }

        public Event? Event { get; set; }

        [Required]
        [MinLength(GlobalConstants.Post.ContentMinLength)]
        [MaxLength(GlobalConstants.Post.ContentMaxLength)]
        public string Content { get; set; } = null!;

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public ICollection<PostImage> Images { get; set; }

        public ICollection<PostComment> Comments { get; set; }

        public ICollection<PostLike> Likes { get; set; }
    }
}
