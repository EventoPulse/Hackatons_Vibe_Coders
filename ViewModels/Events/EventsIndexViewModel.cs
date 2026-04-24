using EventsApp.Models;

namespace EventsApp.ViewModels.Events
{
    public class EventsIndexViewModel
    {
        public string? Search { get; set; }
        public string? City { get; set; }
        public EventGenre? Genre { get; set; }
        public DateTime? DateFrom { get; set; }
        public IReadOnlyList<EventCardViewModel> Events { get; set; } = Array.Empty<EventCardViewModel>();
        public IReadOnlyList<string> Cities { get; set; } = Array.Empty<string>();
    }
}
