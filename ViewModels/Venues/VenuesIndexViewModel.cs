namespace EventsApp.ViewModels.Venues
{
    public class VenuesIndexViewModel
    {
        public string? Search { get; set; }
        public string? City { get; set; }
        public IReadOnlyList<VenueCardViewModel> Venues { get; set; } = Array.Empty<VenueCardViewModel>();
        public IReadOnlyList<string> Cities { get; set; } = Array.Empty<string>();
    }
}
