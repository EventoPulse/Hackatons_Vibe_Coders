namespace EventsApp.ViewModels.Home
{
    public class EventMapMarkerViewModel
    {
        public string City { get; set; } = null!;
        public int EventCount { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
}
