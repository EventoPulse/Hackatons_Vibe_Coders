using System.ComponentModel.DataAnnotations;
using EventsApp.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventsApp.ViewModels.Posts
{
    public class PostCreateEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(GlobalConstants.Post.ContentMaxLength, MinimumLength = GlobalConstants.Post.ContentMinLength)]
        public string Content { get; set; } = null!;

        [Display(Name = "Event (optional)")]
        public int? EventId { get; set; }

        [Url]
        [StringLength(GlobalConstants.Post.ImageUrlMaxLength)]
        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        public IEnumerable<SelectListItem> Events { get; set; } = Array.Empty<SelectListItem>();
    }
}
