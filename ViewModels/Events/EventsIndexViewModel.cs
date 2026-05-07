using EventsApp.Models;
using EventsApp.ViewModels.Home;

namespace EventsApp.ViewModels.Events
{
    public class EventsIndexViewModel
    {
        public string? Search { get; set; }
        public string? City { get; set; }
        public EventGenre? Genre { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string Sort { get; set; } = "recent";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalEventsCount { get; set; }
        public bool HasMoreEvents => Page * PageSize < TotalEventsCount;
        public IReadOnlyList<EventCardViewModel> Events { get; set; } = Array.Empty<EventCardViewModel>();
        public IReadOnlyList<EventMapMarkerViewModel> MapMarkers { get; set; } = Array.Empty<EventMapMarkerViewModel>();
        public IReadOnlyList<string> Cities { get; set; } = Array.Empty<string>();
        public IReadOnlyList<EventCardViewModel> TonightEvents { get; set; } = Array.Empty<EventCardViewModel>();
        public IReadOnlyList<EventCardViewModel> WeekendEvents { get; set; } = Array.Empty<EventCardViewModel>();
        public IReadOnlyList<EventCardViewModel> TrendingEvents { get; set; } = Array.Empty<EventCardViewModel>();
        public IReadOnlyList<EventCardViewModel> RecentlyViewedEvents { get; set; } = Array.Empty<EventCardViewModel>();
        public IReadOnlyList<PopularOrganizerViewModel> PopularOrganizers { get; set; } = Array.Empty<PopularOrganizerViewModel>();
        public IReadOnlyList<PopularCityViewModel> PopularCities { get; set; } = Array.Empty<PopularCityViewModel>();
        public string? PreferredCity { get; set; }
        public bool IsAuthenticated { get; set; }
        public UserOnboardingChecklist? OnboardingChecklist { get; set; }
    }

    public class UserOnboardingChecklist
    {
        public bool HasSavedEvent { get; set; }
        public bool HasAttended { get; set; }
        public bool HasFollowed { get; set; }
        public bool HasViewedEvents { get; set; }
        public bool IsComplete => HasSavedEvent && HasAttended && HasFollowed && HasViewedEvents;
    }
}
