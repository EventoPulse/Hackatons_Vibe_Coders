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
        private readonly ILogger<SearchApiController> _logger;

        public SearchApiController(IAiSearchService ai, IEventSemanticSearchService semanticSearch, ILogger<SearchApiController> logger)
        {
            _ai = ai;
            _semanticSearch = semanticSearch;
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

            var local = LocalEventSearchInterpreter.Parse(query, DateTime.UtcNow);
            AiSearchIntent? intent = local.Intent;
            var usedAi = false;

            if (!_ai.IsEnabled || local.HasStrongIntent || !local.ShouldAskAi)
            {
                ApplyIntent(result, intent);
                ApplyKeywordFallback(result, query);
                result.AiUsed = false;
                result.AiStatus = _ai.IsEnabled ? "Local" : "Disabled";
                result.AiStatusDetail = _ai.IsEnabled
                    ? "Parsed locally without spending AI tokens."
                    : "AI search is not configured. Local smart search was used.";
                return Ok(result);
            }

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

            ApplyIntent(result, intent);
            result.AiUsed = usedAi;

            result.AiStatus = _ai.LastStatus.ToString();
            result.AiStatusDetail = usedAi
                ? _ai.LastStatusDetail
                : (_ai.LastStatusDetail ?? "AI did not improve the local parse; local smart search was used.");

            ApplyCityFallback(result, query);
            ApplyKeywordFallback(result, query);

            await ApplySemanticRankingAsync(result, query, ct);

            return Ok(result);
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
            result.NearMe = intent.NearMe;
            result.Latitude = intent.Latitude;
            result.Longitude = intent.Longitude;
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
