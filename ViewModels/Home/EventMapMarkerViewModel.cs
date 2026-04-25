namespace EventsApp.ViewModels.Home
{
    public class EventMapMarkerViewModel
    {
        public int EventId { get; set; }
        public string Title { get; set; } = null!;
        public string VenueName { get; set; } = null!;
        public string City { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string? ImageUrl { get; set; }
    }
}
