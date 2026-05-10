using EventsApp.Models;

namespace EventsApp.Services.AI
{
    public class AiSearchIntent
    {
        public string? City { get; set; }
        public string[] Cities { get; set; } = Array.Empty<string>();
        public string? Keyword { get; set; }
        public EventGenre? Genre { get; set; }
        public EventGenre[] Genres { get; set; } = Array.Empty<EventGenre>();
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public bool NearMe { get; set; }
        public string? Explanation { get; set; }
        public string? DateIntent { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string[] Keywords { get; set; } = Array.Empty<string>();
        public string? RawQuery { get; set; }
    }

    public enum AiStatus
    {
        Ok,
        Disabled,
        MissingProjectId,
        ProvisionFailed,
        CallFailed,
        ParseFailed,
    }

    public class DayPlanRequestIntent
    {
        public string? City { get; set; }
        public DateTime? Date { get; set; }
        public string? Vibe { get; set; }
        public string? GroupContext { get; set; }
    }

    public class DayPlanTimelineSlot
    {
        public string Slot { get; set; } = "before";
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? EventId { get; set; }
    }

    public class DayPlanTimeline
    {
        public string? Title { get; set; }
        public string? Intro { get; set; }
        public List<DayPlanTimelineSlot> Slots { get; set; } = new();
    }

    public interface IAiSearchService
    {
        bool IsEnabled { get; }
        AiStatus LastStatus { get; }
        string? LastStatusDetail { get; }
        Task<AiSearchIntent?> InterpretAsync(string query, CancellationToken cancellationToken = default);
        Task<string?> GenerateEventDescriptionAsync(string title, string? city, string? genre, string? hints, string? lang = null, CancellationToken cancellationToken = default);
        Task<string?> GenerateTextAsync(string prompt, string tag, CancellationToken cancellationToken = default);
        Task<DayPlanRequestIntent?> ParseDayPlanRequestAsync(string description, IReadOnlyList<string> knownCities, CancellationToken cancellationToken = default);
        Task<DayPlanTimeline?> GenerateDayPlanTimelineAsync(DayPlanRequestIntent intent, IReadOnlyList<DayPlanEventCandidate> events, CancellationToken cancellationToken = default);
    }

    public class DayPlanEventCandidate
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Genre { get; set; }
        public string? Address { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
