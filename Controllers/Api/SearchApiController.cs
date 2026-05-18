using EventsApp.Common;
using EventsApp.Models.AI;
using EventsApp.Services.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace EventsApp.Controllers.Api
{
    [ApiController]
    [Route("api/search")]
    public class SearchApiController : ControllerBase
    {
        // Raised from 180 to 1000. The old cap broke long natural-language
        // queries ("искам да отида на джаз концерт в София този петък в
        // малка зала"). The semantic retriever handles long queries fine
        // since it scores by keyword overlap, not by AI parsing.
        private const int MaxSmartQueryLength = 1000;
        private const int SemanticTopK = 30;

        private readonly IAiSearchService _ai;
        private readonly IEventSemanticSearchService _semanticSearch;
        private readonly IEventSearchFilterService _filterSearch;
        private readonly ILogger<SearchApiController> _logger;

        public SearchApiController(IAiSearchService ai, IEventSemanticSearchService semanticSearch, IEventSearchFilterService filterSearch, ILogger<SearchApiController> logger)
        {
            _ai = ai;
            _semanticSearch = semanticSearch;
            _filterSearch = filterSearch;
            _logger = logger;
        }

        public class SmartSearchRequest
        {
            public string? Query { get; set; }
        }

        [HttpPost("smart")]
        [EnableRateLimiting("ai-light")]
        public async Task<IActionResult> Smart([FromBody] SmartSearchRequest? request, CancellationToken ct)
        {
            var query = (request?.Query ?? string.Empty).Trim();

            var result = new AiSearchResult
            {
                RawQuery = query,
            };

            if (string.IsNullOrWhiteSpace(query))
            {
                result.AiUsed = false;
                result.AiStatus = "Empty";
                return Ok(result);
            }

            if (query.Length > MaxSmartQueryLength)
            {
                query = query[..MaxSmartQueryLength];
            }

            // 1) Always start with the local heuristic parser — fast,
            //    deterministic, and free. It handles common Bulgarian
            //    patterns (городове, "утре", "уикенд", time ranges,
            //    "околието") without spending a single AI token.
            var local = LocalEventSearchInterpreter.Parse(query, DateTime.UtcNow);
            AiSearchIntent? intent = local.Intent;
            var usedAi = false;

            // 2) If the local parse missed structure and the query is
            //    long / fuzzy enough to benefit from AI parsing, call
            //    the LLM as a *parser only* — it returns structured
            //    JSON filters, never event content. AI failures are
            //    swallowed; we fall back to the local intent.
            if (_ai.IsEnabled && !local.HasStrongIntent && local.ShouldAskAi)
            {
                try
                {
                    var aiIntent = await _ai.InterpretAsync(query, ct);
                    if (aiIntent != null)
                    {
                        intent = LocalEventSearchInterpreter.Merge(intent, aiIntent);
                        usedAi = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Smart search AI call threw");
                }
            }

            ApplyIntent(result, intent);
            ApplyCityFallback(result, query);
            ApplyKeywordFallback(result, query);

            // 3) Run the parsed filters against the database. The DB
            //    query is the *only* source of event IDs — the AI never
            //    invents events.
            if (intent != null)
            {
                try
                {
                    var matched = await _filterSearch.SearchAsync(intent, SemanticTopK, ct);
                    result.EventIds = matched.ToArray();
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Structured filter search failed; using semantic fallback");
                }
            }

            // 4) Safety net: if structured filters returned nothing
            //    (e.g. parse failure, sparse data), still give the user
            //    *something* by ranking the whole approved corpus with
            //    the BM25 retriever against the raw query.
            if (result.EventIds.Length == 0)
            {
                await ApplySemanticRankingAsync(result, query, ct);
            }

            result.AiUsed = usedAi;
            result.AiStatus = _ai.IsEnabled ? _ai.LastStatus.ToString() : "Disabled";
            result.AiStatusDetail = usedAi
                ? _ai.LastStatusDetail
                : (_ai.IsEnabled
                    ? (_ai.LastStatusDetail ?? "AI did not improve the local parse; local smart search was used.")
                    : "AI search is not configured. Local smart search was used.");

            result.FilterSummary = BuildSummary(result);

            return Ok(result);
        }

        private static string? BuildSummary(AiSearchResult r)
        {
            var parts = new List<string>(6);
            if (!string.IsNullOrWhiteSpace(r.City)) parts.Add(r.City!);
            else if (r.Cities.Length > 0) parts.Add(string.Join(", ", r.Cities));
            if (r.RadiusKm.HasValue) parts.Add($"в радиус {r.RadiusKm.Value} км");
            if (!string.IsNullOrWhiteSpace(r.Genre)) parts.Add(r.Genre!);
            if (!string.IsNullOrWhiteSpace(r.DateIntent)) parts.Add(r.DateIntent!);
            else if (r.DateFrom.HasValue && r.DateTo.HasValue)
            {
                parts.Add(r.DateFrom.Value.Date == r.DateTo.Value.Date
                    ? r.DateFrom.Value.ToString("dd.MM.yyyy")
                    : $"{r.DateFrom.Value:dd.MM} — {r.DateTo.Value:dd.MM.yyyy}");
            }
            if (!string.IsNullOrEmpty(r.StartTimeOfDay) && !string.IsNullOrEmpty(r.EndTimeOfDay))
                parts.Add($"{r.StartTimeOfDay}–{r.EndTimeOfDay}");
            return parts.Count == 0 ? null : string.Join(" · ", parts);
        }

        // POST /api/search/semantic — pure BM25 retrieval, no AI parsing.
        // Useful when the caller just wants ranked event IDs from a long
        // free-form query without any side effects.
        [HttpPost("semantic")]
        [EnableRateLimiting("public-read")]
        public async Task<IActionResult> Semantic([FromBody] SmartSearchRequest? request, CancellationToken ct)
        {
            var query = (request?.Query ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(query))
            {
                return Ok(new { eventIds = Array.Empty<int>() });
            }

            if (query.Length > MaxSmartQueryLength)
            {
                query = query[..MaxSmartQueryLength];
            }

            try
            {
                var ranked = await _semanticSearch.SearchAsync(query, SemanticTopK, ct);
                return Ok(new
                {
                    eventIds = ranked.Select(r => r.EventId).ToArray(),
                    scores = ranked.Select(r => Math.Round(r.Score, 3)).ToArray(),
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Semantic search failed for query length {Len}", query.Length);
                return Ok(new { eventIds = Array.Empty<int>() });
            }
        }

        private async Task ApplySemanticRankingAsync(AiSearchResult result, string query, CancellationToken ct)
        {
            try
            {
                var ranked = await _semanticSearch.SearchAsync(query, SemanticTopK, ct);
                if (ranked.Count == 0) return;
                result.EventIds = ranked.Select(r => r.EventId).ToArray();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Semantic ranking did not run for smart search");
            }
        }

        private static void ApplyCityFallback(AiSearchResult result, string query)
        {
            if (result.Latitude.HasValue && result.Longitude.HasValue) return;

            string? cityName = result.Cities.FirstOrDefault() ?? result.City;
            if (string.IsNullOrWhiteSpace(cityName))
            {
                foreach (var token in query.Split(new[] { ' ', ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (CityCoordinates.TryGetCoordinates(token, out _, out _))
                    {
                        cityName = token;
                        break;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(cityName) &&
                CityCoordinates.TryGetCoordinates(cityName, out var lat, out var lng))
            {
                result.Latitude ??= lat;
                result.Longitude ??= lng;
                result.City ??= cityName;
                if (result.Cities.Length == 0) result.Cities = new[] { cityName };
            }
        }

        private static void ApplyIntent(AiSearchResult result, AiSearchIntent? intent)
        {
            if (intent == null) return;

            result.City = intent.City;
            result.Cities = intent.Cities.Length > 0
                ? intent.Cities
                : (string.IsNullOrWhiteSpace(intent.City) ? Array.Empty<string>() : new[] { intent.City });
            result.Genre = intent.Genre?.ToString();
            result.Genres = intent.Genres.Length > 0
                ? intent.Genres.Select(g => g.ToString()).ToArray()
                : (intent.Genre.HasValue ? new[] { intent.Genre.Value.ToString() } : Array.Empty<string>());
            result.Keyword = intent.Keyword ?? result.Keyword;
            result.DateIntent = intent.DateIntent;
            result.DateFrom = intent.DateFrom;
            result.DateTo = intent.DateTo;
            result.NearMe = intent.NearMe;
            result.Latitude = intent.Latitude;
            result.Longitude = intent.Longitude;
            result.RadiusKm = intent.RadiusKm;
            result.StartTimeOfDay = intent.StartTimeOfDay?.ToString(@"hh\:mm");
            result.EndTimeOfDay = intent.EndTimeOfDay?.ToString(@"hh\:mm");
            result.Keywords = intent.Keywords ?? Array.Empty<string>();
        }

        private static void ApplyKeywordFallback(AiSearchResult result, string query)
        {
            var stop = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "искам", "търся", "събития", "събитие", "events", "event", "near", "around", "около", "на"
            };
            var compact = string.Join(' ', query
                .Trim()
                .ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(part => !stop.Contains(part)));

            if (!string.IsNullOrWhiteSpace(result.Keyword))
            {
                result.Keyword = result.Keyword.Trim();
                return;
            }

            var hasStructuredFilters =
                !string.IsNullOrWhiteSpace(result.City) ||
                result.Cities.Length > 0 ||
                !string.IsNullOrWhiteSpace(result.Genre) ||
                result.Genres.Length > 0 ||
                !string.IsNullOrWhiteSpace(result.DateIntent) ||
                result.NearMe ||
                result.Latitude.HasValue ||
                result.Longitude.HasValue;

            if (result.AiUsed && hasStructuredFilters)
            {
                result.Keyword = null;
                result.Keywords = Array.Empty<string>();
                return;
            }

            result.Keyword = string.IsNullOrWhiteSpace(compact) ? (string.IsNullOrWhiteSpace(query) ? null : query) : compact;
        }
    }
}
