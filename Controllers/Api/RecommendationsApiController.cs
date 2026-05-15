using EventsApp.Data;
using EventsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EventsApp.Controllers.Api
{
    [ApiController]
    [Route("api/recommendations")]
    [IgnoreAntiforgeryToken]
    public class RecommendationsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public RecommendationsApiController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // POST /api/recommendations/preview
        // Live tuner preview — accepts the full UserPreferences shape from the front-end
        // and returns total count + top 10 events with match scores.
        [HttpPost("preview")]
        [EnableRateLimiting("public-read")]
        public async Task<IActionResult> Preview([FromBody] RecPreviewRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var now = DateTime.UtcNow;

            var weights = (request.Genres ?? new List<RecGenreWeight>())
                .Where(g => !string.IsNullOrWhiteSpace(g.Genre))
                .ToDictionary(
                    g => g.Genre!,
                    g => Math.Clamp(g.Weight <= 0 ? 3 : g.Weight, 1, 5),
                    StringComparer.OrdinalIgnoreCase);

            var pickedGenres = new HashSet<EventGenre>();
            foreach (var g in weights.Keys)
            {
                if (Enum.TryParse<EventGenre>(g, ignoreCase: true, out var enumVal))
                {
                    pickedGenres.Add(enumVal);
                }
            }

            var cities = (request.Cities ?? new List<string>())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim().ToLower())
                .ToList();

            var times = (request.Times ?? new List<string>())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.ToLowerInvariant())
                .ToHashSet();

            var distanceKm = request.DistanceKm > 0 ? request.DistanceKm : 100;
            var budgetBgn = request.BudgetBgn;

            var query = _db.Events
                .AsNoTracking()
                .Where(e => e.IsApproved && e.StartTime > now)
                .Include(e => e.Likes)
                .Include(e => e.Saves)
                .Include(e => e.Attendances)
                .Include(e => e.Tickets)
                .Include(e => e.OrganizerProfile)
                .Include(e => e.Organizer)
                .AsQueryable();

            if (cities.Count > 0)
            {
                query = query.Where(e => cities.Contains(e.City.ToLower()));
            }

            var candidates = await query.Take(400).ToListAsync();

            // Score in memory
            var scored = candidates.Select(e =>
            {
                double score = 40; // base

                // Genre match (0–30)
                if (pickedGenres.Count == 0)
                {
                    score += 15;
                }
                else if (pickedGenres.Contains(e.Genre))
                {
                    var w = weights.TryGetValue(e.Genre.ToString(), out var ww) ? ww : 3;
                    score += 12 + w * 3.6;
                }

                // Time of day (0–10)
                if (times.Count == 0 || times.Contains("any"))
                {
                    score += 6;
                }
                else
                {
                    var hour = e.StartTime.Hour;
                    var dow = e.StartTime.DayOfWeek;
                    bool ok = false;
                    if (times.Contains("morning") && hour >= 6 && hour < 11) ok = true;
                    if (times.Contains("afternoon") && hour >= 11 && hour < 17) ok = true;
                    if (times.Contains("evening") && hour >= 17 && hour < 22) ok = true;
                    if (times.Contains("night") && (hour >= 22 || hour < 2)) ok = true;
                    if (times.Contains("latenight") && hour >= 2 && hour < 6) ok = true;
                    if (times.Contains("weekend") && (dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday)) ok = true;
                    if (times.Contains("weekday") && dow >= DayOfWeek.Monday && dow <= DayOfWeek.Friday) ok = true;
                    if (ok) score += 10;
                }

                // Budget (0–10)
                if (budgetBgn <= 0)
                {
                    if (e.Tickets.Count == 0 || e.Tickets.All(t => t.Price == 0))
                    {
                        score += 10;
                    }
                    else
                    {
                        score -= 8;
                    }
                }
                else
                {
                    var minPrice = e.Tickets.Count == 0 ? 0 : e.Tickets.Min(t => (double)t.Price);
                    if (minPrice <= budgetBgn) score += 10;
                    else score -= 6;
                }

                // Social signals (0–10)
                int going = e.Attendances.Count(a => a.Status == EventAttendanceStatus.Going);
                int likes = e.Likes.Count;
                if (request.Social?.Trending == true)
                {
                    score += Math.Min(8, (likes + going) / 4.0);
                }

                // Vibe — crowdSize tilts toward low/high attendance
                if (request.Vibe != null)
                {
                    var crowd = request.Vibe.CrowdSize;
                    var crowdBias = crowd / 50.0 - 1.0; // -1..1
                    score += crowdBias * Math.Min(6, going / 10.0);
                }

                int finalScore = (int)Math.Round(Math.Clamp(score, 5, 99));

                var reasons = new List<object>();
                if (pickedGenres.Contains(e.Genre))
                {
                    reasons.Add(new { icon = "bi-music-note-beamed", label = $"Слушаш {e.Genre}", kind = "" });
                }
                if (going >= 50)
                {
                    reasons.Add(new { icon = "bi-graph-up-arrow", label = $"+{going} отиват", kind = "is-time" });
                }
                if (e.StartTime.Hour >= 22 || e.StartTime.Hour < 4)
                {
                    if (times.Contains("night") || times.Contains("latenight"))
                    {
                        reasons.Add(new { icon = "bi-moon-stars", label = "След полунощ", kind = "is-time" });
                    }
                }

                return new
                {
                    ev = e,
                    score = finalScore,
                    reasons,
                    friendsGoing = 0,
                };
            })
            .Where(x => x.score >= 35)
            .OrderByDescending(x => x.score)
            .ThenBy(x => x.ev.StartTime)
            .ToList();

            var topMatches = scored.Take(10).Select(s => MapMatch(s.ev, userId, s.score, s.reasons, s.friendsGoing)).ToList();

            return Ok(new
            {
                count = scored.Count,
                topMatches,
            });
        }

        private static object MapMatch(Event e, string? userId, int match, List<object> reasons, int friendsGoing) => new
        {
            id = e.Id,
            title = e.Title,
            description = e.Description,
            startTime = e.StartTime,
            endTime = e.EndTime,
            genre = e.Genre.ToString(),
            imageUrl = e.ImageUrl,
            address = e.Address,
            city = e.City,
            latitude = e.Latitude,
            longitude = e.Longitude,
            organizerName = e.OrganizerProfile?.DisplayName ?? "",
            organizerProfileId = e.OrganizerProfileId,
            likesCount = e.Likes.Count,
            savesCount = e.Saves.Count,
            goingCount = e.Attendances.Count(a => a.Status == EventAttendanceStatus.Going),
            interestedCount = e.Attendances.Count(a => a.Status == EventAttendanceStatus.Interested),
            isLiked = userId != null && e.Likes.Any(l => l.UserId == userId),
            isSaved = userId != null && e.Saves.Any(s => s.UserId == userId),
            userAttendanceStatus = userId != null
                ? e.Attendances.FirstOrDefault(a => a.UserId == userId)?.Status.ToString()
                : null,
            match,
            reasons,
            friendsGoing,
        };
    }

    public class RecPreviewRequest
    {
        public List<RecGenreWeight>? Genres { get; set; }
        public RecVibe? Vibe { get; set; }
        public List<string>? Times { get; set; }
        public int DistanceKm { get; set; }
        public int BudgetBgn { get; set; }
        public List<string>? Cities { get; set; }
        public RecSocial? Social { get; set; }
        public string? Preset { get; set; }
    }

    public class RecGenreWeight
    {
        public string? Genre { get; set; }
        public int Weight { get; set; }
    }

    public class RecVibe
    {
        public int CrowdSize { get; set; }
        public int Energy { get; set; }
        public int Discovery { get; set; }
    }

    public class RecSocial
    {
        public bool Friends { get; set; }
        public bool Trending { get; set; }
        public bool SavedLike { get; set; }
    }
}
