using EventsApp.ViewModels.Events;
using EventsApp.ViewModels.Posts;

namespace EventsApp.ViewModels.Home
{
    public class HomeIndexViewModel
    {
        public IReadOnlyList<EventCardViewModel> LatestEvents { get; set; } = Array.Empty<EventCardViewModel>();
        public IReadOnlyList<PostCardViewModel> LatestPosts { get; set; } = Array.Empty<PostCardViewModel>();
        public IReadOnlyList<EventMapMarkerViewModel> MapMarkers { get; set; } = Array.Empty<EventMapMarkerViewModel>();
        public bool ShowPreferencesPrompt { get; set; }
    }
}
