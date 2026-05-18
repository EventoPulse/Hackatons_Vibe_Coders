using EventsApp.Data;
using EventsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Services.AI
{
    /// <summary>
    /// In-memory BM25 retriever over approved events. The existing AI
    /// "smart search" only extracts structured filters (city, genre,
    /// date) and fails on long natural-language queries; this service
    /// gives every search a ranked list of candidate events derived from
    /// keyword overlap, so a long sentence like
    /// "искам да отида на джаз концерт в София този петък в малка зала"
    /// still scores the right events highly even when the AI fails to
    /// parse any single filter.
    ///
    /// This is the retrieval half of RAG. The corpus is refreshed every
    /// few minutes (or on explicit invalidation) so writes don't have to
    /// block on indexing.
    /// </summary>
    public interface IEventSemanticSearchService
    {
        Task<IReadOnlyList<RankedEvent>> SearchAsync(string query, int topK, CancellationToken cancellationToken = default);
        void Invalidate();
    }

    public readonly record struct RankedEvent(int EventId, double Score);

    public sealed class EventSemanticSearchService : IEventSemanticSearchService
    {
        // Tuned defaults from the BM25 paper. Good baseline for short
        // documents like event cards.
        private const double K1 = 1.5;
        private const double B = 0.75;
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EventSemanticSearchService> _logger;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        private volatile Index _index = new(Array.Empty<EventDoc>(), new Dictionary<string, int>(), 1d);
        private DateTime _builtAt = DateTime.MinValue;

        public EventSemanticSearchService(IServiceScopeFactory scopeFactory, ILogger<EventSemanticSearchService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public void Invalidate() => _builtAt = DateTime.MinValue;

        public async Task<IReadOnlyList<RankedEvent>> SearchAsync(string query, int topK, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query) || topK <= 0)
            {
                return Array.Empty<RankedEvent>();
            }

            await EnsureFreshAsync(cancellationToken);
            var idx = _index;
            if (idx.Docs.Length == 0) return Array.Empty<RankedEvent>();

            var queryTokens = Tokenize(query);
            if (queryTokens.Length == 0) return Array.Empty<RankedEvent>();

            // Pre-compute IDF for the query terms once.
            var n = idx.Docs.Length;
            var idf = new Dictionary<string, double>(queryTokens.Length);
            foreach (var term in queryTokens.Distinct())
            {
                var df = idx.DocumentFrequency.GetValueOrDefault(term, 0);
                // BM25 IDF — same formula Lucene uses; the +1 inside the
                // log keeps the score positive when a term appears in
                // more than half the docs.
                idf[term] = Math.Log((n - df + 0.5) / (df + 0.5) + 1);
            }

            var scored = new List<RankedEvent>(Math.Min(n, 64));
            foreach (var doc in idx.Docs)
            {
                double score = 0;
                foreach (var term in queryTokens)
                {
                    if (!doc.Frequencies.TryGetValue(term, out var tf)) continue;
                    var termIdf = idf[term];
                    if (termIdf <= 0) continue;
                    var lengthNorm = 1 - B + B * (doc.Length / idx.AverageDocLength);
                    var weight = tf * (K1 + 1) / (tf + K1 * lengthNorm);
                    score += termIdf * weight;
                }
                if (score > 0) scored.Add(new RankedEvent(doc.EventId, score));
            }

            scored.Sort(static (a, b) => b.Score.CompareTo(a.Score));
            return scored.Count <= topK ? scored : scored.GetRange(0, topK);
        }

        private async Task EnsureFreshAsync(CancellationToken cancellationToken)
        {
            if (DateTime.UtcNow - _builtAt < CacheTtl) return;
            await _refreshLock.WaitAsync(cancellationToken);
            try
            {
                if (DateTime.UtcNow - _builtAt < CacheTtl) return;
                await RebuildAsync(cancellationToken);
                _builtAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to rebuild event semantic search index");
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        private async Task RebuildAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Materialise everything we want to search over in one query.
            // Approved events only — pending/rejected entries shouldn't
            // surface in public search results.
            var rows = await db.Events
                .AsNoTracking()
                .Where(e => e.IsApproved)
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.Description,
                    e.City,
                    e.Address,
                    Genre = e.Genre.ToString(),
                    OrganizerName = e.OrganizerProfile != null ? e.OrganizerProfile.DisplayName : null,
                    Tickets = e.Tickets
                        .Where(t => t.IsActive)
                        .Select(t => new { t.Name, t.Description })
                        .ToList(),
                })
                .ToListAsync(cancellationToken);

            var docs = new EventDoc[rows.Count];
            var df = new Dictionary<string, int>();
            long totalLen = 0;

            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                // Concatenate every searchable surface form. Title is
                // doubled so it weighs more than the description body.
                var pieces = new List<string?>
                {
                    r.Title, r.Title,
                    r.Description,
                    r.City, r.City,
                    r.Address,
                    r.Genre, r.Genre,
                    r.OrganizerName,
                };
                foreach (var t in r.Tickets)
                {
                    pieces.Add(t.Name);
                    pieces.Add(t.Description);
                }
                var text = string.Join(' ', pieces.Where(p => !string.IsNullOrWhiteSpace(p)));
                var tokens = Tokenize(text);

                var tf = new Dictionary<string, int>(tokens.Length);
                foreach (var tk in tokens)
                    tf[tk] = tf.GetValueOrDefault(tk) + 1;

                docs[i] = new EventDoc(r.Id, tokens.Length, tf);
                totalLen += tokens.Length;

                foreach (var term in tf.Keys)
                    df[term] = df.GetValueOrDefault(term) + 1;
            }

            var avgLen = docs.Length > 0 ? Math.Max(1.0, (double)totalLen / docs.Length) : 1d;
            _index = new Index(docs, df, avgLen);
            _logger.LogInformation("Rebuilt event search index: {Count} events, {Terms} distinct terms, avg length {Avg:F1}.", docs.Length, df.Count, avgLen);
        }

        private static readonly char[] Delimiters =
        {
            ' ', '\t', '\n', '\r', '\f',
            '.', ',', ';', ':', '!', '?',
            '(', ')', '[', ']', '{', '}',
            '/', '\\', '|', '"', '\'', '`',
            '-', '_', '+', '=', '*', '#', '@', '&', '<', '>',
        };

        // Conservative stop-word list. Bulgarian + English glue words
        // that carry zero retrieval signal in this domain. Names like
        // "Sofia" or "rock" never appear here, so we don't lose recall.
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            // Bulgarian connectors / pronouns / common verbs
            "и","в","на","за","с","от","до","по","към","или","но","а","че","да","не",
            "съм","си","сме","сте","са","ли","се","ме","ми","му","му","им","ти","той","тя",
            "то","ние","вие","те","този","това","тази","тези","един","една","едно",
            "искам","търся","ходя","ходим","отида","отидем","днес","утре","вчера",
            "този","петък","събота","неделя","вечер","сутрин","събитие","събития",
            // English glue
            "the","a","an","and","or","of","to","in","on","at","is","are","was","were",
            "with","for","by","this","that","it","be","i","you","we","they","my","your",
            "event","events","near","around","tonight","today","tomorrow","yesterday",
        };

        private static string[] Tokenize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
            var lower = text.ToLowerInvariant();
            var parts = lower.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries);
            // length >= 2 drops single characters / digits that only add noise
            return parts
                .Where(p => p.Length >= 2 && !StopWords.Contains(p))
                .ToArray();
        }

        // Immutable per-rebuild snapshot so a read in progress is never
        // torn by a concurrent rebuild. Replace `_index` atomically.
        private sealed record Index(EventDoc[] Docs, Dictionary<string, int> DocumentFrequency, double AverageDocLength);

        private sealed record EventDoc(int EventId, int Length, Dictionary<string, int> Frequencies);
    }
}
