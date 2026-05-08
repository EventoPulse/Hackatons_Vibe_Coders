namespace EventsApp.Services
{
    public interface IPushNotificationService
    {
        string? PublicKey { get; }

        bool IsConfigured { get; }

        Task SendNotificationAsync(
            string recipientUserId,
            string title,
            string body,
            string url,
            string tag = "evento",
            int? badgeCount = null,
            CancellationToken cancellationToken = default);

        Task SendMessageNotificationAsync(
            string recipientUserId,
            string title,
            string body,
            string url,
            int? badgeCount = null,
            CancellationToken cancellationToken = default);
    }
}
