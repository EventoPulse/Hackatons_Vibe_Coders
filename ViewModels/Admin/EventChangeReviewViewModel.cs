using EventsApp.Models;
using EventsApp.ViewModels.Events;

namespace EventsApp.ViewModels.Admin
{
    public class EventChangeReviewViewModel
    {
        public int EventId { get; set; }
        public string CurrentTitle { get; set; } = null!;
        public string CurrentCity { get; set; } = null!;
        public string CurrentAddress { get; set; } = null!;
        public DateTime CurrentStartTime { get; set; }
        public DateTime CurrentEndTime { get; set; }
        public EventGenre CurrentGenre { get; set; }
        public string? CurrentImageUrl { get; set; }
        public DateTime SubmittedAt { get; set; }
        public EventPendingChangePayload Pending { get; set; } = null!;
    }
}
