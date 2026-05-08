using EventsApp.Models;
using EventsApp.ViewModels.Events;
using EventsApp.ViewModels.Posts;

namespace EventsApp.ViewModels.Organizer
{
    public class OrganizerDashboardViewModel
    {
        public bool HasProfile { get; set; }
        public string? OrganizationName { get; set; }
        public string? Description { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Website { get; set; }
        public string? CompanyNumber { get; set; }
        public bool Approved { get; set; }
        public int? ActiveWorkspaceId { get; set; }
        public int? ActivePageId { get; set; }
        public string? ActiveWorkspaceName { get; set; }
        public string? ActivePageName { get; set; }
        public string? PaymentStatus { get; set; }
        public IReadOnlyList<OrganizerWorkspaceRowViewModel> Workspaces { get; set; } = Array.Empty<OrganizerWorkspaceRowViewModel>();
        public IReadOnlyList<OrganizerPageContextRowViewModel> Pages { get; set; } = Array.Empty<OrganizerPageContextRowViewModel>();

        public int EventsCount { get; set; }
        public int PostsCount { get; set; }
        public int TicketTypesCount { get; set; }
        public int TicketsSoldCount { get; set; }
        public int EventsWithTicketsCount { get; set; }
        public int LayoutsCount { get; set; }

        public int UpcomingEventsCount { get; set; }
        public int PastEventsCount { get; set; }
        public int TicketsUsedCount { get; set; }
        public int TotalLikes { get; set; }
        public int TotalComments { get; set; }
        public int TotalViews { get; set; }
        public int Last30DaysViews { get; set; }
        public int VipBoostCreditsAvailable { get; set; }
        public int VipBoostCreditsUsed { get; set; }
        public bool ShowFirstBoostNotice { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageTicketPrice { get; set; }
        public int Last30DaysSold { get; set; }
        public decimal Last30DaysRevenue { get; set; }

        public IReadOnlyList<TopEventStat> TopByTicketsSold { get; set; } = Array.Empty<TopEventStat>();
        public IReadOnlyList<TopEventStat> TopByRevenue { get; set; } = Array.Empty<TopEventStat>();
        public IReadOnlyList<TopEventStat> TopByViews { get; set; } = Array.Empty<TopEventStat>();
        public IReadOnlyList<GenreCountStat> GenreBreakdown { get; set; } = Array.Empty<GenreCountStat>();
        public IReadOnlyList<CityCountStat> CityBreakdown { get; set; } = Array.Empty<CityCountStat>();
        public IReadOnlyList<DailySalesPoint> SalesLast30Days { get; set; } = Array.Empty<DailySalesPoint>();

        public IReadOnlyList<EventCardViewModel> RecentEvents { get; set; } = Array.Empty<EventCardViewModel>();
        public IReadOnlyList<PostCardViewModel> RecentPosts { get; set; } = Array.Empty<PostCardViewModel>();
        public IReadOnlyList<OrganizerEventTicketRowViewModel> EventTicketRows { get; set; } = Array.Empty<OrganizerEventTicketRowViewModel>();
        public IReadOnlyList<OrganizerSoldTicketRowViewModel> SoldTickets { get; set; } = Array.Empty<OrganizerSoldTicketRowViewModel>();
        public IReadOnlyList<OrganizerEngagementActivityViewModel> RecentEngagement { get; set; } = Array.Empty<OrganizerEngagementActivityViewModel>();
    }

    public class OrganizerWorkspaceRowViewModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = null!;
        public string LegalName { get; set; } = null!;
        public string? CompanyNumber { get; set; }
        public bool IsDefault { get; set; }
        public bool ChargesEnabled { get; set; }
        public bool PayoutsEnabled { get; set; }
        public PaymentProvider PaymentProvider { get; set; }
    }

    public class OrganizerPageContextRowViewModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = null!;
        public int? BusinessWorkspaceId { get; set; }
        public bool IsDefaultForWorkspace { get; set; }
        public bool IsActive { get; set; }
    }

    public class OrganizerEventTicketRowViewModel
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public bool HasActiveTickets { get; set; }
        public int Sold { get; set; }
        public int Likes { get; set; }
        public int Comments { get; set; }
        public int Views { get; set; }
        public int UniqueViewers { get; set; }
        public int VipBoostScore { get; set; }
        public bool CanBoost { get; set; }
        public decimal Revenue { get; set; }
    }

    public class OrganizerSoldTicketRowViewModel
    {
        public Guid Id { get; set; }
        public int EventId { get; set; }
        public string EventTitle { get; set; } = null!;
        public string TicketName { get; set; } = null!;
        public string BuyerEmail { get; set; } = null!;
        public string? AttendeeName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime PurchasedAt { get; set; }
        public decimal PricePaid { get; set; }
        public bool IsUsed { get; set; }
    }

    public class OrganizerEngagementActivityViewModel
    {
        public string Type { get; set; } = null!;
        public string ActorName { get; set; } = null!;
        public int EventId { get; set; }
        public string EventTitle { get; set; } = null!;
        public string? Text { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class OrganizerEventsViewModel
    {
        public IReadOnlyList<OrganizerEventRowViewModel> Events { get; set; } = Array.Empty<OrganizerEventRowViewModel>();
        public int VipBoostCreditsAvailable { get; set; }
        public int VipBoostCreditsUsed { get; set; }
    }

    public class OrganizerEventRowViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string City { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public bool IsApproved { get; set; }
        public bool HasPendingChanges { get; set; }
        public string OrganizerPageName { get; set; } = null!;
        public int TicketsCount { get; set; }
        public int SoldTicketsCount { get; set; }
        public int VipBoostScore { get; set; }
    }

    public class TopEventStat
    {
        public int EventId { get; set; }
        public string Title { get; set; } = null!;
        public int Sold { get; set; }
        public decimal Revenue { get; set; }
        public int Views { get; set; }
        public int UniqueViewers { get; set; }
        public int EngagementScore { get; set; }
    }

    public class GenreCountStat
    {
        public EventGenre Genre { get; set; }
        public int Count { get; set; }
    }

    public class CityCountStat
    {
        public string City { get; set; } = null!;
        public int Count { get; set; }
    }

    public class DailySalesPoint
    {
        public DateTime Date { get; set; }
        public int Sold { get; set; }
        public decimal Revenue { get; set; }
    }
}
