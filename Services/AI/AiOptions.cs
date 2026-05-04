namespace EventsApp.Services.AI
{
    public class AiOptions
    {
        public const string SectionName = "AI";

        public string ApiKey { get; set; } = string.Empty;
        public string ModelName { get; set; } = "gpt-4.1-mini";
        public int TimeoutSeconds { get; set; } = 20;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
    }
}
