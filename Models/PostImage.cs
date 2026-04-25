using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventsApp.Common;

namespace EventsApp.Models
{
    public class PostImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Post))]
        public int PostId { get; set; }

        public Post Post { get; set; } = null!;

        [Required]
        [MaxLength(GlobalConstants.Post.ImageUrlMaxLength)]
        public string ImageUrl { get; set; } = null!;

        [Required]
        public PostMediaType MediaType { get; set; }
    }
}
