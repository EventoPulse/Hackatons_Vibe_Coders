using EventsApp.ViewModels.Events;
using EventsApp.ViewModels.Posts;
using EventsApp.ViewModels.Venues;

namespace EventsApp.ViewModels.Search
{
    public class SearchResultsViewModel
    {
        public string? Query { get; set; }
        public IReadOnlyList<EventCardViewModel> Events { get; set; } = Array.Empty<EventCardViewModel>();
        public IReadOnlyList<VenueCardViewModel> Venues { get; set; } = Array.Empty<VenueCardViewModel>();
        public IReadOnlyList<PostCardViewModel> Posts { get; set; } = Array.Empty<PostCardViewModel>();
        public string? AiHint { get; set; }
    }
}
