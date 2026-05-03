using System.ComponentModel.DataAnnotations;
using EventsApp.Common;
using Microsoft.AspNetCore.Http;

namespace EventsApp.ViewModels.Social
{
    public class StoryCreateViewModel
    {
        [Display(Name = "Image or video URL")]
        [StringLength(GlobalConstants.Social.StoryMediaUrlMaxLength)]
        public string? MediaUrl { get; set; }

        [Display(Name = "Upload")]
        public IFormFile? MediaFile { get; set; }

        [StringLength(GlobalConstants.Social.StoryCaptionMaxLength)]
        public string? Caption { get; set; }

        public string? ActiveWorkspaceName { get; set; }

        public string? ActivePageName { get; set; }
    }
}
