using EventsApp.ViewModels.Events;
using EventsApp.ViewModels.Posts;

namespace EventsApp.ViewModels.Organizer
{
    public class OrganizerDashboardViewModel
    {
        public bool HasProfile { get; set; }
        public string? OrganizationName { get; set; }
        public string? Description { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Website { get; set; }
        public string? CompanyNumber { get; set; }
        public bool Approved { get; set; }
        public int EventsCount { get; set; }
        public int PostsCount { get; set; }
        public int TicketTypesCount { get; set; }
        public int TicketsSoldCount { get; set; }
        public int EventsWithTicketsCount { get; set; }
        public IReadOnlyList<EventCardViewModel> RecentEvents { get; set; } = Array.Empty<EventCardViewModel>();
        public IReadOnlyList<PostCardViewModel> RecentPosts { get; set; } = Array.Empty<PostCardViewModel>();
        public IReadOnlyList<OrganizerEventTicketRowViewModel> EventTicketRows { get; set; } = Array.Empty<OrganizerEventTicketRowViewModel>();
    }

    public class OrganizerEventTicketRowViewModel
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public bool HasActiveTickets { get; set; }
        public int Sold { get; set; }
    }
}
