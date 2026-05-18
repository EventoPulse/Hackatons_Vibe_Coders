using System.ComponentModel.DataAnnotations;

namespace EventsApp.Models.AI
{
    /// <summary>
    /// Persistent cache for parsed AI search intents. Every unique
    /// normalized query that ever made it past the local heuristics
    /// and through the OpenAI parser ends up here. Subsequent identical
    /// (after normalization) queries are served from this table instead
    /// of paying for another OpenAI call.
    ///
    /// This keeps the system learning over time: a Bulgarian colloquial
    /// reference the local parser doesn't know — "малката Виена",
    /// "града с най-голямата ТВ кула", etc. — gets resolved once by
    /// the AI and then stays resolved forever.
    /// </summary>
    public class SearchIntentCacheEntry
    {
        [Key]
        public int Id { get; set; }

        /// <summary>SHA-256 hex of the normalized query. Unique index.</summary>
        [Required]
        [MaxLength(64)]
        public string QueryHash { get; set; } = string.Empty;

        /// <summary>The normalized query string. Kept for debugging / audits.</summary>
        [Required]
        [MaxLength(1024)]
        public string NormalizedQuery { get; set; } = string.Empty;

        /// <summary>Serialized AiSearchIntent JSON.</summary>
        [Required]
        public string IntentJson { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

        public int HitCount { get; set; } = 0;
    }
}
