using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventsApp.Common;

namespace EventsApp.Models
{
    public class Story
    {
        public Story()
        {
            this.CreatedAt = DateTime.UtcNow;
            this.ExpiresAt = this.CreatedAt.AddHours(24);
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Author))]
        public string AuthorId { get; set; } = null!;

        public ApplicationUser Author { get; set; } = null!;

        [ForeignKey(nameof(OrganizerProfile))]
        public int? OrganizerProfileId { get; set; }

        public OrganizerProfile? OrganizerProfile { get; set; }

        [ForeignKey(nameof(BusinessWorkspace))]
        public int? BusinessWorkspaceId { get; set; }

        public BusinessWorkspace? BusinessWorkspace { get; set; }

        [Required]
        [MaxLength(GlobalConstants.Social.StoryMediaUrlMaxLength)]
        public string MediaUrl { get; set; } = null!;

        [Required]
        public PostMediaType MediaType { get; set; }

        [MaxLength(GlobalConstants.Social.StoryCaptionMaxLength)]
        public string? Caption { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime ExpiresAt { get; set; }
    }
}
