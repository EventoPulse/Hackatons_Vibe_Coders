using EventsApp.Common;
using EventsApp.Data;
using EventsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Controllers.Api
{
    [ApiController]
    [Route("api/wrapped")]
    [IgnoreAntiforgeryToken]
    [Authorize(Policy = "ApiAuth")]
    public class WrappedApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public WrappedApiController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int? year)
        {
            var userId = _userManager.GetUserId(User)!;
            var selectedYear = year ?? DateTime.UtcNow.Year;
            var from = new DateTime(selectedYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = from.AddYears(1);

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

            var attended = await _db.EventAttendances
                .AsNoTracking()
                .Where(a => a.UserId == userId && a.Status == EventAttendanceStatus.Going && a.Event.StartTime >= from && a.Event.StartTime < to)
                .Include(a => a.Event).ThenInclude(e => e.Organizer)
                .Select(a => a.Event)
                .ToListAsync();

            var topGenre = attended
                .GroupBy(e => e.Genre)
                .OrderByDescending(g => g.Count())
                .Select(g => new { genre = g.Key.ToString(), count = g.Count() })
                .FirstOrDefault();

            var topCity = attended
                .GroupBy(e => e.City)
                .OrderByDescending(g => g.Count())
                .Select(g => new { city = g.Key, count = g.Count() })
                .FirstOrDefault();

            var topOrganizer = attended
                .GroupBy(e => e.Organizer.UserName ?? e.OrganizerId)
                .OrderByDescending(g => g.Count())
                .Select(g => new { organizer = g.Key, count = g.Count() })
                .FirstOrDefault();

            var busiestMonth = attended
                .GroupBy(e => e.StartTime.Month)
                .OrderByDescending(g => g.Count())
                .Select(g => new { month = g.Key, count = g.Count() })
                .FirstOrDefault();

            var commentsPosted = await _db.EventComments.CountAsync(c => c.UserId == userId && c.CreatedAt >= from && c.CreatedAt < to)
                + await _db.PostComments.CountAsync(c => c.UserId == userId && c.CreatedAt >= from && c.CreatedAt < to);

            var likesGiven = await _db.EventLikes.CountAsync(l => l.UserId == userId && l.CreatedAt >= from && l.CreatedAt < to)
                + await _db.PostLikes.CountAsync(l => l.UserId == userId && l.CreatedAt >= from && l.CreatedAt < to);

            return Ok(new
            {
                year = selectedYear,
                displayName = user?.FirstName ?? user?.UserName ?? "Evento",
                totalEventsAttended = attended.Count,
                totalHoursOnScene = attended.Sum(e => Math.Max(1, (int)Math.Round((e.EndTime - e.StartTime).TotalHours))),
                citiesVisited = attended.Select(e => e.City).Distinct().Count(),
                organizersFollowed = await _db.Follows.CountAsync(f => f.FollowerId == userId),
                likesGiven,
                commentsPosted,
                topGenre = topGenre?.genre,
                topGenreCount = topGenre?.count ?? 0,
                topCity = topCity?.city,
                topCityCount = topCity?.count ?? 0,
                topOrganizer = topOrganizer?.organizer,
                topOrganizerCount = topOrganizer?.count ?? 0,
                busiestMonth = busiestMonth?.month,
                busiestMonthCount = busiestMonth?.count ?? 0,
                topEvents = attended
                    .OrderByDescending(e => e.StartTime)
                    .Take(5)
                    .Select(e => new
                    {
                        id = e.Id,
                        title = e.Title,
                        city = e.City,
                        startTime = e.StartTime,
                        imageUrl = e.ImageUrl,
                        genre = e.Genre.ToString(),
                    }),
            });
        }
    }
}
