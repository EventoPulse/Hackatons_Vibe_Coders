using EventsApp.Common;
using EventsApp.Data;
using EventsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Services.AI
{
    /// <summary>
    /// Applies a parsed <see cref="AiSearchIntent"/> against the database
    /// and returns matching event IDs ranked by relevance + soonest start.
    /// This is the second half of the structured search pipeline: the AI
    /// (or the local parser) extracts filters → this service runs them
    /// against real events. The AI never sees the events; it only
    /// produces filters. This keeps the system from inventing results.
    /// </summary>
    public interface IEventSearchFilterService
    {
        Task<IReadOnlyList<int>> SearchAsync(AiSearchIntent intent, int topK, CancellationToken cancellationToken = default);
    }

    public sealed class EventSearchFilterService : IEventSearchFilterService
    {
        // Loose bounding box for the 30 km default. At Bulgarian latitude
        // (~43°) one degree of latitude is ~111 km and one degree of
        // longitude ~81 km, so we pad generously and then trim with a
        // proper Haversine check in memory.
        private const double LatDegPerKm = 1.0 / 111.0;
        private const double LngDegPerKm = 1.0 / 81.0;

        private readonly ApplicationDbContext _db;
        private readonly IEventSemanticSearchService _semantic;
        private readonly ILogger<EventSearchFilterService> _logger;

        public EventSearchFilterService(
            ApplicationDbContext db,
            IEventSemanticSearchService semantic,
            ILogger<EventSearchFilterService> logger)
        {
            _db = db;
            _semantic = semantic;
            _logger = logger;
        }

        public async Task<IReadOnlyList<int>> SearchAsync(AiSearchIntent intent, int topK, CancellationToken cancellationToken = default)
        {
            if (topK <= 0) return Array.Empty<int>();

            var sofia = TryGetSofiaTimeZone();
            var nowUtc = DateTime.UtcNow;

            // === Step 1: build the base IQueryable with everything EF can
            // translate to SQL. The radius and the time-of-day window are
            // applied in memory after a tight bounding-box pre-filter,
            // because EF cannot translate Haversine or DST-aware time
            // arithmetic. ===
            // Track whether any structured filter was applied — when the
            // user typed e.g. "малката виена" and we didn't manage to
            // resolve it to a city, the rest of the search must NOT
            // silently return random upcoming events. Better to return
            // empty so the UI can show a proper "не намерих" empty
            // state.
            var hasStructuredFilter = false;
            IQueryable<Event> q = _db.Events
                .AsNoTracking()
                .Where(e => e.IsApproved && e.OrganizerProfileId != null && e.StartTime >= nowUtc);

            // Date range — interpret the intent's DateFrom / DateTo as
            // Europe/Sofia calendar days, convert to UTC, and apply.
            if (intent.DateFrom.HasValue || intent.DateTo.HasValue)
            {
                var from = intent.DateFrom ?? intent.DateTo!.Value;
                var to = intent.DateTo ?? intent.DateFrom!.Value;
                // Treat the supplied date as a Sofia-local midnight.
                var fromUtc = ToUtcDayStart(from, sofia);
                var toUtc = ToUtcDayStart(to.AddDays(1), sofia); // exclusive upper bound
                q = q.Where(e => e.StartTime >= fromUtc && e.StartTime < toUtc);
                hasStructuredFilter = true;
            }

            // City filter — compare against the normalized City column.
            // Note: Event.City is stored as the typed-in string, which
            // may be Bulgarian ("София") or English ("Sofia"). To match
            // both, build an OR list over the equivalent forms of every
            // requested city.
            var cityVariants = ExpandCityVariants(intent);
            if (cityVariants.Count > 0)
            {
                // SQL: e.City IN (var1, var2, ...) case-insensitive.
                var lowered = cityVariants.Select(c => c.ToLower()).ToArray();
                q = q.Where(e => lowered.Contains(e.City.ToLower()));
                hasStructuredFilter = true;
            }

            // Genre filter — accepts either the single .Genre or any of
            // the .Genres array.
            var genres = intent.Genres.Length > 0
                ? intent.Genres
                : (intent.Genre.HasValue ? new[] { intent.Genre.Value } : Array.Empty<EventGenre>());
            if (genres.Length > 0)
            {
                q = q.Where(e => genres.Contains(e.Genre));
                hasStructuredFilter = true;
            }

            // Radius pre-filter: bounding box in SQL, refined in memory.
            double? radiusCenterLat = null, radiusCenterLng = null;
            if (intent.RadiusKm.HasValue)
            {
                radiusCenterLat = intent.Latitude;
                radiusCenterLng = intent.Longitude;
                if ((!radiusCenterLat.HasValue || !radiusCenterLng.HasValue)
                    && !string.IsNullOrWhiteSpace(intent.City)
                    && CityCoordinates.TryGetCoordinates(intent.City, out var cityLat, out var cityLng))
                {
                    radiusCenterLat = cityLat;
                    radiusCenterLng = cityLng;
                }
                if (radiusCenterLat.HasValue && radiusCenterLng.HasValue)
                {
                    var padKm = intent.RadiusKm.Value + 5;
                    var latPad = padKm * LatDegPerKm;
                    var lngPad = padKm * LngDegPerKm;
                    var minLat = radiusCenterLat.Value - latPad;
                    var maxLat = radiusCenterLat.Value + latPad;
                    var minLng = radiusCenterLng.Value - lngPad;
                    var maxLng = radiusCenterLng.Value + lngPad;
                    q = q.Where(e => e.Latitude != null && e.Longitude != null
                        && e.Latitude >= minLat && e.Latitude <= maxLat
                        && e.Longitude >= minLng && e.Longitude <= maxLng);
                    hasStructuredFilter = true;
                }
            }

            // Pull a generous slice — over-fetch by 3x so the in-memory
            // trims (radius + time-of-day) still leave us with enough
            // hits to return topK.
            var prelimTake = Math.Max(topK * 3, 50);
            var preliminary = await q
                .OrderBy(e => e.StartTime)
                .Take(prelimTake)
                .Select(e => new EventRow(
                    e.Id,
                    e.StartTime,
                    e.Latitude,
                    e.Longitude,
                    e.Likes.Count + e.Attendances.Count(a => a.Status == EventAttendanceStatus.Going) * 2))
                .ToListAsync(cancellationToken);

            // === Step 2: apply in-memory refinements. ===

            // Radius: Haversine distance must be within RadiusKm.
            if (radiusCenterLat.HasValue && radiusCenterLng.HasValue && intent.RadiusKm.HasValue)
            {
                var maxKm = intent.RadiusKm.Value;
                preliminary = preliminary
                    .Where(e => e.Latitude.HasValue && e.Longitude.HasValue
                        && HaversineKm(radiusCenterLat.Value, radiusCenterLng.Value, e.Latitude.Value, e.Longitude.Value) <= maxKm)
                    .ToList();
            }

            // Time-of-day window: filter events whose Sofia-local start
            // hour falls inside [start, end]. UTC ↔ Sofia conversion is
            // DST-aware via TimeZoneInfo.ConvertTimeFromUtc.
            if (intent.StartTimeOfDay.HasValue && intent.EndTimeOfDay.HasValue && sofia != null)
            {
                var startOfDay = intent.StartTimeOfDay.Value;
                var endOfDay = intent.EndTimeOfDay.Value;
                preliminary = preliminary
                    .Where(e =>
                    {
                        var localStart = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(e.StartTime, DateTimeKind.Utc), sofia);
                        var tod = localStart.TimeOfDay;
                        return tod >= startOfDay && tod <= endOfDay;
                    })
                    .ToList();
            }

            // === Step 3: rank. If the intent carries a keyword, mix BM25
            // score with the soonest-start preference. Otherwise just
            // sort by StartTime ascending and break ties on engagement. ===
            var keywordQuery = string.IsNullOrWhiteSpace(intent.Keyword)
                ? (intent.Keywords.Length > 0 ? string.Join(' ', intent.Keywords) : null)
                : intent.Keyword;

            if (!string.IsNullOrWhiteSpace(keywordQuery))
            {
                try
                {
                    var ranked = await _semantic.SearchAsync(keywordQuery, Math.Max(topK * 4, 100), cancellationToken);
                    // When the keyword is the *only* signal we have
                    // (no city, no genre, no date, no radius) and BM25
                    // returns nothing, we explicitly return empty
                    // instead of padding random upcoming events with a
                    // 0.05 baseline score. The previous behavior was
                    // misleading: a query like "малката виена" with no
                    // matches showed 30 unrelated events as if the
                    // search had succeeded.
                    if (ranked.Count == 0 && !hasStructuredFilter)
                    {
                        return Array.Empty<int>();
                    }

                    var scoreById = ranked.ToDictionary(r => r.EventId, r => r.Score);
                    // If we have BOTH structured filters AND a keyword,
                    // events without a BM25 hit can still surface (they
                    // already passed the structured filter) — but with
                    // a tiny baseline so keyword hits float to the top.
                    return preliminary
                        .Select(e => new
                        {
                            e.Id,
                            Score = (scoreById.TryGetValue(e.Id, out var s) ? s : 0.05)
                                    + e.Engagement * 0.002
                                    - DaysFromNow(e.StartTime) * 0.05,
                        })
                        .OrderByDescending(x => x.Score)
                        .Take(topK)
                        .Select(x => x.Id)
                        .ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "BM25 reranking failed; falling back to date sort");
                }
            }

            // No keyword at all → no need to rerank. Just return what
            // the structured filters picked, sorted by soonest first.
            // If there were ALSO no structured filters, return empty —
            // showing the whole upcoming-events corpus for an empty
            // intent is not what a search should do.
            if (!hasStructuredFilter)
            {
                return Array.Empty<int>();
            }

            return preliminary
                .OrderBy(e => e.StartTime)
                .ThenByDescending(e => e.Engagement)
                .Take(topK)
                .Select(e => e.Id)
                .ToList();
        }

        private static double DaysFromNow(DateTime utc)
            => (utc - DateTime.UtcNow).TotalDays;

        // Returns the equivalent display names for every requested city,
        // so a search for "Ruse" also matches an event whose City column
        // is literally "Русе". CityCoordinates.GetEquivalentNames knows
        // both spellings.
        private static List<string> ExpandCityVariants(AiSearchIntent intent)
        {
            var seeds = new List<string>();
            if (!string.IsNullOrWhiteSpace(intent.City)) seeds.Add(intent.City);
            foreach (var c in intent.Cities)
            {
                if (!string.IsNullOrWhiteSpace(c)) seeds.Add(c);
            }
            if (seeds.Count == 0) return new List<string>();

            var variants = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var seed in seeds)
            {
                variants.Add(seed.Trim());
                foreach (var eq in CityCoordinates.GetEquivalentNames(seed))
                {
                    variants.Add(eq);
                }
            }
            return variants.ToList();
        }

        private static DateTime ToUtcDayStart(DateTime date, TimeZoneInfo? tz)
        {
            var sofiaMidnight = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Unspecified);
            if (tz == null)
            {
                return DateTime.SpecifyKind(sofiaMidnight, DateTimeKind.Utc);
            }
            return TimeZoneInfo.ConvertTimeToUtc(sofiaMidnight, tz);
        }

        private static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371.0; // earth radius in km
            var dLat = ToRadians(lat2 - lat1);
            var dLng = ToRadians(lng2 - lng1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                  + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                  * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double ToRadians(double deg) => deg * Math.PI / 180.0;

        private static TimeZoneInfo? TryGetSofiaTimeZone()
        {
            foreach (var id in new[] { "Europe/Sofia", "FLE Standard Time" })
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }
            return null;
        }

        private readonly record struct EventRow(int Id, DateTime StartTime, double? Latitude, double? Longitude, int Engagement);
    }
}
