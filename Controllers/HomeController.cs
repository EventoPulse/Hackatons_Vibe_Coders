using System.Diagnostics;
using EventsApp.Data;
using EventsApp.Models;
using EventsApp.ViewModels.Events;
using EventsApp.ViewModels.Home;
using EventsApp.ViewModels.Posts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Controllers
{
    public class HomeController : Controller
    {
        private const int LatestCount = 6;

        private readonly ApplicationDbContext _db;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext db, ILogger<HomeController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.UtcNow;

            var events = await _db.Events
                .AsNoTracking()
                .Where(e => e.IsApproved && e.StartTime >= now)
                .OrderBy(e => e.StartTime)
                .Take(LatestCount)
                .Select(e => new EventCardViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    ImageUrl = e.ImageUrl,
                    VenueName = e.Venue.Name,
                    VenueCity = e.Venue.City,
                    StartTime = e.StartTime,
                    Genre = e.Genre,
                    IsApproved = e.IsApproved,
                    OrganizerId = e.OrganizerId,
                    OrganizerName = e.Organizer.UserName ?? string.Empty,
                    LikesCount = e.Likes.Count,
                    CommentsCount = e.Comments.Count,
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
                    FirstImageUrl = p.Images.Select(i => i.ImageUrl).FirstOrDefault(),
                    LikesCount = p.Likes.Count,
                    CommentsCount = p.Comments.Count,
                })
                .ToListAsync();

            return View(new HomeIndexViewModel
            {
                LatestEvents = events,
                LatestPosts = posts,
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
