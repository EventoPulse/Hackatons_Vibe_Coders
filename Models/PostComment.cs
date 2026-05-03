using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventsApp.Common;

namespace EventsApp.Models
{
    public class PostComment
    {
        public PostComment()
        {
            this.CreatedAt = DateTime.UtcNow;
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Post))]
        public int PostId { get; set; }

        public Post Post { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(User))]
        public string UserId { get; set; } = null!;

        public ApplicationUser User { get; set; } = null!;

        public AuthorIdentityType AuthorType { get; set; } = AuthorIdentityType.User;

        [ForeignKey(nameof(AuthorOrganizerProfile))]
        public int? AuthorOrganizerProfileId { get; set; }

        public OrganizerProfile? AuthorOrganizerProfile { get; set; }

        [ForeignKey(nameof(BusinessWorkspace))]
        public int? BusinessWorkspaceId { get; set; }

        public BusinessWorkspace? BusinessWorkspace { get; set; }

        [Required]
        [MinLength(GlobalConstants.Comment.ContentMinLength)]
        [MaxLength(GlobalConstants.Comment.ContentMaxLength)]
        public string Content { get; set; } = null!;

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
