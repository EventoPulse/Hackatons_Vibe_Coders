using EventsApp.Common;
using EventsApp.Data;
using EventsApp.Models;
using EventsApp.ViewModels.Events;
using EventsApp.ViewModels.Posts;
using EventsApp.ViewModels.Search;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Controllers
{
    public class SearchController : Controller
    {
        private const int ResultsPerSection = 12;

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public SearchController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? q)
        {
            var vm = new SearchResultsViewModel { Query = q };

            if (string.IsNullOrWhiteSpace(q))
            {
                return View(vm);
            }

            var term = q.Trim();
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole(GlobalConstants.Roles.Admin);

            var eventsQuery = _db.Events
                .AsNoTracking()
                .Where(e => isAdmin || e.IsApproved)
                .Where(e =>
                    e.Title.Contains(term)
                    || (e.Description != null && e.Description.Contains(term))
                    || e.Address.Contains(term)
                    || e.City.Contains(term));

            vm.Events = await eventsQuery
                .OrderBy(e => e.StartTime)
                .Take(ResultsPerSection)
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

            vm.Posts = await _db.Posts
                .AsNoTracking()
                .Where(p => p.Content.Contains(term) || (p.Event != null && p.Event.Title.Contains(term)))
                .OrderByDescending(p => p.CreatedAt)
                .Take(ResultsPerSection)
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

            var totalHits = vm.Events.Count + vm.Posts.Count;
            vm.AiHint = totalHits == 0
                ? "Smart AI search is on the way — try a simpler keyword for now."
                : "Smart AI search arriving soon. Until then, results are matched by keyword.";

            return View(vm);
        }
    }
}
