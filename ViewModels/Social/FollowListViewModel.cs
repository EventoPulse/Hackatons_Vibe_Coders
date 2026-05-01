namespace EventsApp.ViewModels.Social
{
    public class FollowListViewModel
    {
        public string ProfileId { get; set; } = null!;
        public string ProfileName { get; set; } = null!;
        public string ListTitle { get; set; } = null!;
        public IReadOnlyList<ProfileSummaryViewModel> Profiles { get; set; } = Array.Empty<ProfileSummaryViewModel>();
    }
}
