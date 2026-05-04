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
        public IReadOnlyList<FeedSearchResultViewModel> SearchResults { get; set; } = Array.Empty<FeedSearchResultViewModel>();
        public string? SearchQuery { get; set; }
        public bool HasPersonalSignals { get; set; }
        public string? PreferredCity { get; set; }
        public string? CurrentUserDisplayName { get; set; }
        public string? CurrentUserProfileImageUrl { get; set; }
    }

    public class FeedSearchResultViewModel
    {
        public string Id { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public int? OrganizerProfileId { get; set; }
        public string DisplayName { get; set; } = null!;
        public string? UserName { get; set; }
        public string? Bio { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string TypeKey { get; set; } = "profile.type.profile";
        public string TypeText { get; set; } = "Profile";
        public int FollowersCount { get; set; }
        public int PostsCount { get; set; }
        public int EventsCount { get; set; }
        public bool CurrentUserFollows { get; set; }
    }
}
