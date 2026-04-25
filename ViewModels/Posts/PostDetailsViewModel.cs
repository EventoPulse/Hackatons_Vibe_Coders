namespace EventsApp.ViewModels.Posts
{
    public class PostDetailsViewModel
    {
        public int Id { get; set; }
        public string OrganizerId { get; set; } = null!;
        public string OrganizerName { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? EventId { get; set; }
        public string? EventTitle { get; set; }
        public IReadOnlyList<PostMediaItemViewModel> Media { get; set; } = Array.Empty<PostMediaItemViewModel>();
        public int LikesCount { get; set; }
        public bool CurrentUserLiked { get; set; }
        public IReadOnlyList<PostCommentViewModel> Comments { get; set; } = Array.Empty<PostCommentViewModel>();
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    public class PostCommentViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool CanDelete { get; set; }
    }
}
