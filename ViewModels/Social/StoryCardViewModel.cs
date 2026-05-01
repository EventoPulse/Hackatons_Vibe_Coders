using EventsApp.Models;

namespace EventsApp.ViewModels.Social
{
    public class StoryCardViewModel
    {
        public int Id { get; set; }
        public string AuthorId { get; set; } = null!;
        public string AuthorName { get; set; } = null!;
        public string? AuthorImageUrl { get; set; }
        public string MediaUrl { get; set; } = null!;
        public PostMediaType MediaType { get; set; }
        public string? Caption { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
