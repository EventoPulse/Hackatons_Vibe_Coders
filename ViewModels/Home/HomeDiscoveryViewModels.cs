namespace EventsApp.ViewModels.Home
{
    public class PopularOrganizerViewModel
    {
        public int Id { get; set; }

        public string OwnerId { get; set; } = null!;

        public string DisplayName { get; set; } = null!;

        public string? City { get; set; }

        public string? Tagline { get; set; }

        public string? AvatarImageUrl { get; set; }

        public int UpcomingEventsCount { get; set; }
    }

    public class PopularCityViewModel
    {
        public string Name { get; set; } = null!;

        public int EventsCount { get; set; }
    }
}
