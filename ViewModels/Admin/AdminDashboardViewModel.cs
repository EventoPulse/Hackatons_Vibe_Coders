using EventsApp.ViewModels.Posts;

namespace EventsApp.ViewModels.Admin
{
    public class AdminDashboardViewModel
    {
        public int UsersCount { get; set; }
        public int OrganizersCount { get; set; }
        public int EventsCount { get; set; }
        public int PendingOrganizersCount { get; set; }
        public int PendingEventsCount { get; set; }
        public IReadOnlyList<PostCardViewModel> RecentPosts { get; set; } = Array.Empty<PostCardViewModel>();
    }
}
