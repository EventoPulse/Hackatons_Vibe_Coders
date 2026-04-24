namespace EventsApp.ViewModels.Posts
{
    public class PostCardViewModel
    {
        public int Id { get; set; }
        public string OrganizerId { get; set; } = null!;
        public string OrganizerName { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public int? EventId { get; set; }
        public string? EventTitle { get; set; }
        public string? FirstImageUrl { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
    }
}
