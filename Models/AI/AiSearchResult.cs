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
        // Top event IDs ranked by the in-memory BM25 retriever. Populated
        // for every smart query so the client can render an answer even
        // when the AI couldn't pull a structured filter out of a long
        // natural-language sentence.
        public int[] EventIds { get; set; } = System.Array.Empty<int>();
    }
}
