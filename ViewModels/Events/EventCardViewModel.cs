using EventsApp.Models;

namespace EventsApp.ViewModels.Events
{
    public class EventCardViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public string VenueName { get; set; } = null!;
        public string VenueCity { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public EventGenre Genre { get; set; }
        public bool IsApproved { get; set; }
        public string OrganizerId { get; set; } = null!;
        public string OrganizerName { get; set; } = null!;
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
    }
}
