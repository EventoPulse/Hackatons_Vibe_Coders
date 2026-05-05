using EventsApp.ViewModels.Events;
using EventsApp.ViewModels.Posts;

namespace EventsApp.ViewModels.Social
{
    public class PublicOrganizerPageViewModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = null!;
        public string? Tagline { get; set; }
        public string? Description { get; set; }
        public string? City { get; set; }
        public string? AvatarImageUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? Website { get; set; }
        public string? InstagramUrl { get; set; }
        public string? FacebookUrl { get; set; }
        public string? TikTokUrl { get; set; }
        public bool CanMessagePage { get; set; }
        public IReadOnlyList<EventCardViewModel> Events { get; set; } = Array.Empty<EventCardViewModel>();
        public IReadOnlyList<PostCardViewModel> Posts { get; set; } = Array.Empty<PostCardViewModel>();
    }
}
