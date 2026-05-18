using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EventsApp.Data;
using EventsApp.Models.AI;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Services.AI
{
    /// <summary>
    /// Persistent learning cache for AI search intents. The idea: a
    /// Bulgarian colloquial / trivia query the local parser doesn't
    /// understand ("малката Виена", "градът с най-голямата ТВ кула",
    /// "Розовата долина") is sent to OpenAI once. The structured
    /// JSON intent it returns is stored here, keyed by SHA-256 of the
    /// normalized query. The next time the same (or a stylistically
    /// equivalent) query arrives, we return the cached intent and
    /// never call OpenAI.
    ///
    /// This is the "system learns over time" behavior — the codebase
    /// doesn't need a hand-written list of every Bulgarian nickname;
    /// production traffic + GPT-4 fills in the long tail.
    /// </summary>
    public interface IPersistentSearchIntentCache
    {
        Task<AiSearchIntent?> TryGetAsync(string query, CancellationToken cancellationToken = default);
        Task StoreAsync(string query, AiSearchIntent intent, CancellationToken cancellationToken = default);
    }

    public sealed class PersistentSearchIntentCache : IPersistentSearchIntentCache
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PersistentSearchIntentCache> _logger;

        public PersistentSearchIntentCache(IServiceScopeFactory scopeFactory, ILogger<PersistentSearchIntentCache> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<AiSearchIntent?> TryGetAsync(string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query)) return null;
            var (hash, normalized) = HashKey(query);
            if (hash == null) return null;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var entry = await db.SearchIntentCacheEntries
                    .AsTracking()
                    .FirstOrDefaultAsync(e => e.QueryHash == hash, cancellationToken);
                if (entry == null) return null;

                AiSearchIntent? intent;
                try
                {
                    intent = JsonSerializer.Deserialize<AiSearchIntent>(entry.IntentJson, JsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Persistent intent cache: dropping malformed row for {Normalized}", normalized);
                    db.SearchIntentCacheEntries.Remove(entry);
                    await db.SaveChangesAsync(cancellationToken);
                    return null;
                }

                if (intent == null) return null;

                // Update telemetry — wrap in try/catch because losing a
                // hit-count bump should never break the search.
                try
                {
                    entry.HitCount += 1;
                    entry.LastUsedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Persistent intent cache: hit-count bump failed");
                }

                return intent;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Persistent intent cache: read failed");
                return null;
            }
        }

        public async Task StoreAsync(string query, AiSearchIntent intent, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query) || intent == null) return;
            var (hash, normalized) = HashKey(query);
            if (hash == null) return;

            // Don't cache empty / useless intents — saves space and
            // forces a re-try the next time someone asks.
            if (string.IsNullOrWhiteSpace(intent.City)
                && intent.Cities.Length == 0
                && !intent.Genre.HasValue
                && intent.Genres.Length == 0
                && !intent.DateFrom.HasValue
                && !intent.DateTo.HasValue
                && !intent.NearMe
                && !intent.RadiusKm.HasValue
                && !intent.StartTimeOfDay.HasValue
                && !intent.EndTimeOfDay.HasValue
                && string.IsNullOrWhiteSpace(intent.Keyword)
                && intent.Keywords.Length == 0)
            {
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var existing = await db.SearchIntentCacheEntries
                    .FirstOrDefaultAsync(e => e.QueryHash == hash, cancellationToken);

                var json = JsonSerializer.Serialize(intent, JsonOptions);
                if (existing == null)
                {
                    db.SearchIntentCacheEntries.Add(new SearchIntentCacheEntry
                    {
                        QueryHash = hash,
                        NormalizedQuery = normalized!,
                        IntentJson = json,
                        CreatedAt = DateTime.UtcNow,
                        LastUsedAt = DateTime.UtcNow,
                        HitCount = 0,
                    });
                }
                else
                {
                    existing.IntentJson = json;
                    existing.LastUsedAt = DateTime.UtcNow;
                }
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Race with another request that inserted the same hash
                // a millisecond ago. The other write is fine; ours is a
                // no-op.
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Persistent intent cache: write failed");
            }
        }

        // Public so OpenAiService and the controller can produce the
        // same key in unit tests / diagnostics.
        public static (string? Hash, string? Normalized) HashKey(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return (null, null);
            var normalized = LocalEventSearchInterpreter.Normalize(query);
            if (normalized.Length == 0) return (null, null);
            // Cap at 1024 chars to match the column max length.
            if (normalized.Length > 1024) normalized = normalized[..1024];
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
            return (Convert.ToHexString(bytes).ToLowerInvariant(), normalized);
        }
    }
}
