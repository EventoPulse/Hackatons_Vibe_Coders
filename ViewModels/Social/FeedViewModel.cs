using EventsApp.ViewModels.Events;
using EventsApp.ViewModels.Posts;

namespace EventsApp.ViewModels.Social
{
    public class FeedViewModel
    {
        public IReadOnlyList<StoryCardViewModel> Stories { get; set; } = Array.Empty<StoryCardViewModel>();
        public IReadOnlyList<EventCardViewModel> RecommendedEvents { get; set; } = Array.Empty<EventCardViewModel>();
        public IReadOnlyList<EventCardViewModel> TrendingEvents { get; set; } = Array.Empty<EventCardViewModel>();
        public IReadOnlyList<PostCardViewModel> FriendsActivity { get; set; } = Array.Empty<PostCardViewModel>();
        public IReadOnlyList<PostCardViewModel> OrganizerPosts { get; set; } = Array.Empty<PostCardViewModel>();
        public IReadOnlyList<ProfileSummaryViewModel> SuggestedProfiles { get; set; } = Array.Empty<ProfileSummaryViewModel>();
        public bool HasPersonalSignals { get; set; }
        public string? PreferredCity { get; set; }
    }
}
