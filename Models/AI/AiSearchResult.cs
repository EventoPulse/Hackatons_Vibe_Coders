namespace EventsApp.Models.AI
{
    public class AiSearchResult
    {
        public string? City { get; set; }
        public string[] Cities { get; set; } = System.Array.Empty<string>();
        public string? Genre { get; set; }
        public string[] Genres { get; set; } = System.Array.Empty<string>();
        public string? Keyword { get; set; }
        public string? DateIntent { get; set; }
        public bool NearMe { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string[] Keywords { get; set; } = System.Array.Empty<string>();
        public string RawQuery { get; set; } = string.Empty;
        public bool AiUsed { get; set; }
        public string? AiStatus { get; set; }
        public string? AiStatusDetail { get; set; }
        // Top event IDs after running the parsed filters against the
        // database. Ranked by relevance + soonest start. The client uses
        // this list directly — no second client-side filter pass is
        // required. Empty when the search produced no matches.
        public int[] EventIds { get; set; } = System.Array.Empty<int>();
        public System.DateTime? DateFrom { get; set; }
        public System.DateTime? DateTo { get; set; }
        // Radius around (Latitude, Longitude) in kilometres. Set when
        // the user wrote "околието", "наблизо", "около мен", etc.
        public int? RadiusKm { get; set; }
        // Time-of-day filter in Europe/Sofia local time. "13:00".
        public string? StartTimeOfDay { get; set; }
        public string? EndTimeOfDay { get; set; }
        // Friendly Bulgarian summary of what the system actually
        // filtered on. Built server-side so the UI doesn't have to
        // re-construct it from the structured fields.
        public string? FilterSummary { get; set; }
    }
}
