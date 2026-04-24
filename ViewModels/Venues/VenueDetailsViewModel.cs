using EventsApp.ViewModels.Events;

namespace EventsApp.ViewModels.Venues
{
    public class VenueDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string Address { get; set; } = null!;
        public string City { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public string OrganizerId { get; set; } = null!;
        public string OrganizerName { get; set; } = null!;
        public IReadOnlyList<EventCardViewModel> Events { get; set; } = Array.Empty<EventCardViewModel>();
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }
}
