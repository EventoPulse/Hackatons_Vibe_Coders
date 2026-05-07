using EventsApp.Models;

namespace EventsApp.ViewModels.Events
{
    public class EventPendingChangePayload
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string City { get; set; } = null!;
        public string Address { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public EventGenre Genre { get; set; }
        public int? OrganizerProfileId { get; set; }
        public int? BusinessWorkspaceId { get; set; }
        public string? ImageUrl { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public EventRecurrenceType RecurrenceType { get; set; } = EventRecurrenceType.None;
        public int RecurrenceInterval { get; set; } = 1;
        public List<DayOfWeek> SelectedDaysOfWeek { get; set; } = new();
        public EventOccurrenceDisplayMode OccurrenceDisplayMode { get; set; } = EventOccurrenceDisplayMode.ShowAllDates;
        public DateTime? RecurrenceStartDate { get; set; }
        public DateTime? RecurrenceEndDate { get; set; }
        public TimeSpan? RecurrenceStartTime { get; set; }
        public TimeSpan? RecurrenceEndTime { get; set; }
        public string TimeZone { get; set; } = "Europe/Sofia";
        public RecurringEditScope RecurringEditScope { get; set; } = RecurringEditScope.FutureOccurrences;
        public EventTicketingMode TicketingMode { get; set; } = EventTicketingMode.GeneralAdmission;
        public int? VenueLayoutId { get; set; }
    }
}
