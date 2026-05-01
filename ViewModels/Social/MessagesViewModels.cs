namespace EventsApp.ViewModels.Social
{
    public class ConversationListItemViewModel
    {
        public int Id { get; set; }
        public string OtherUserId { get; set; } = null!;
        public string OtherUserName { get; set; } = null!;
        public string? OtherUserImageUrl { get; set; }
        public string? LastMessage { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int UnseenCount { get; set; }
    }

    public class MessagesIndexViewModel
    {
        public IReadOnlyList<ConversationListItemViewModel> Conversations { get; set; } = Array.Empty<ConversationListItemViewModel>();
    }

    public class MessageBubbleViewModel
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = null!;
        public string SenderName { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? SeenAt { get; set; }
        public bool IsMine { get; set; }
    }

    public class ConversationDetailsViewModel
    {
        public int Id { get; set; }
        public string OtherUserId { get; set; } = null!;
        public string OtherUserName { get; set; } = null!;
        public string? OtherUserImageUrl { get; set; }
        public IReadOnlyList<MessageBubbleViewModel> Messages { get; set; } = Array.Empty<MessageBubbleViewModel>();
    }
}
