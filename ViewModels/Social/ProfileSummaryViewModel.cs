namespace EventsApp.ViewModels.Social
{
    public class ProfileSummaryViewModel
    {
        public string Id { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string? Bio { get; set; }
        public string? ProfileImageUrl { get; set; }
        public bool IsOrganizer { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
        public int PostsCount { get; set; }
        public int EventsCount { get; set; }
        public bool CurrentUserFollows { get; set; }
    }
}
