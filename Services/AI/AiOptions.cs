namespace EventsApp.Services.AI
{
    public class AiOptions
    {
        public const string SectionName = "AI";

        public string ApiKey { get; set; } = string.Empty;
        public string ModelName { get; set; } = "gpt-4.1-mini";
        public int TimeoutSeconds { get; set; } = 20;
        public int MaxSearchQueryLength { get; set; } = 180;
        public int SearchCacheMinutes { get; set; } = 60;
        public int MaxPromptCharacters { get; set; } = 2000;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
    }
}
