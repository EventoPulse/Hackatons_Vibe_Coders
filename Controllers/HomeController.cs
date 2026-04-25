using System.Diagnostics;
using EventsApp.Common;
using EventsApp.Data;
using EventsApp.Models;
using EventsApp.ViewModels.Events;
using EventsApp.ViewModels.Home;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Controllers
{
    public class HomeController : Controller
    {
        private const int LatestCount = 6;
        private const int MapMarkerCount = 30;

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            ILogger<HomeController> logger)
        {
            _db = db;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? search, string? city, EventGenre? genre, DateTime? dateFrom)
        {
            var isAdmin = User.IsInRole(GlobalConstants.Roles.Admin);
            var userId = _userManager.GetUserId(User);

            var events = await _db.Events
                .AsNoTracking()
                .Where(e => e.IsApproved && e.StartTime >= now)
                .OrderBy(e => e.StartTime)
                .Select(e => new EventCardViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    ImageUrl = e.ImageUrl,
                    Address = e.Address,
                    City = e.City,
                    StartTime = e.StartTime,
                    Genre = e.Genre,
                    IsApproved = e.IsApproved,
                    OrganizerId = e.OrganizerId,
                    OrganizerName = e.Organizer.UserName ?? string.Empty,
                    LikesCount = e.Likes.Count,
                    CommentsCount = e.Comments.Count,
                    CurrentUserLiked = userId != null && e.Likes.Any(l => l.UserId == userId),
                })
                .ToListAsync();

            var posts = await _db.Posts
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Take(LatestCount)
                .Select(p => new PostCardViewModel
                {
                    Id = p.Id,
                    OrganizerId = p.OrganizerId,
                    OrganizerName = p.Organizer.UserName ?? string.Empty,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt,
                    EventId = p.EventId,
                    EventTitle = p.Event != null ? p.Event.Title : null,
                    FirstMediaUrl = p.Images.Select(i => i.ImageUrl).FirstOrDefault(),
                    FirstMediaType = p.Images.Select(i => i.MediaType).FirstOrDefault(),
                    LikesCount = p.Likes.Count,
                    CommentsCount = p.Comments.Count,
                    CurrentUserLiked = userId != null && p.Likes.Any(l => l.UserId == userId),
                })
                .ToListAsync();

            var upcomingForMap = await _db.Events
                .AsNoTracking()
                .Where(e => e.IsApproved && e.StartTime >= now)
                .OrderBy(e => e.StartTime)
                .Take(MapMarkerCount)
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.ImageUrl,
                    VenueName = e.Venue.Name,
                    VenueCity = e.Venue.City,
                    e.StartTime,
                })
                .ToListAsync();

            var markers = upcomingForMap
                .Where(e => CityCoordinates.TryGetCoordinates(e.VenueCity, out _, out _))
                .Select(e =>
                {
                    CityCoordinates.TryGetCoordinates(e.City, out var lat, out var lng);
                    return new EventMapMarkerViewModel
                    {
                        City = e.City,
                        EventCount = e.EventCount,
                        Lat = lat,
                        Lng = lng,
                    };
                })
                .OrderBy(m => m.City)
                .ToList();

            var showPrefsPrompt = false;
            if (userId != null
                && !User.IsInRole(GlobalConstants.Roles.Admin)
                && !User.IsInRole(GlobalConstants.Roles.Organizer))
            {
                showPrefsPrompt = !await _db.UserPreferences.AnyAsync(p => p.UserId == userId);
            }

            return View(new EventsIndexViewModel
            {
                LatestEvents = events,
                LatestPosts = posts,
                MapMarkers = markers,
                Cities = cities,
            });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            });
        }
    }
}
