namespace EventsApp.ViewModels.Social
{
    public class ActingIdentityOptionViewModel
    {
        public string Key { get; set; } = null!;

        public string Label { get; set; } = null!;

        public string DisplayName { get; set; } = null!;

        public string? ImageUrl { get; set; }

        public string BadgeKey { get; set; } = "identity.user";

        public bool IsDefault { get; set; }
    }
}
