using EventsApp.Common;
using EventsApp.Data;
using EventsApp.Models;
using EventsApp.ViewModels.Events;
using EventsApp.ViewModels.Organizer;
using EventsApp.ViewModels.Posts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Controllers
{
    [Authorize(Roles = GlobalConstants.Roles.Admin + "," + GlobalConstants.Roles.Organizer)]
    public class OrganizerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrganizerController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User)!;

            var orgData = await _db.OrganizerData
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrganizerId == userId);

            if (orgData == null)
            {
                return RedirectToAction(nameof(Profile));
            }

            var recentEvents = await _db.Events
                .AsNoTracking()
                .Where(e => e.OrganizerId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .Take(5)
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
                    CurrentUserLiked = e.Likes.Any(l => l.UserId == userId),
                })
                .ToListAsync();

            var recentPosts = await _db.Posts
                .AsNoTracking()
                .Where(p => p.OrganizerId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
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
                })
                .ToListAsync();

            var ticketTypesCount = await _db.Tickets
                .CountAsync(t => t.Event.OrganizerId == userId);
            var ticketsSoldCount = await _db.UserTickets
                .CountAsync(ut => ut.Ticket.Event.OrganizerId == userId);
            var eventsWithTickets = await _db.Events
                .CountAsync(e => e.OrganizerId == userId && e.Tickets.Any(t => t.IsActive));

            var eventTicketRows = await _db.Events
                .AsNoTracking()
                .Where(e => e.OrganizerId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .Take(8)
                .Select(e => new OrganizerEventTicketRowViewModel
                {
                    EventId = e.Id,
                    EventTitle = e.Title,
                    StartTime = e.StartTime,
                    HasActiveTickets = e.Tickets.Any(t => t.IsActive),
                    Sold = e.Tickets.SelectMany(t => t.UserTickets).Count(),
                })
                .ToListAsync();

            var vm = new OrganizerDashboardViewModel
            {
                HasProfile = true,
                OrganizationName = orgData.OrganizationName,
                Description = orgData.Description,
                PhoneNumber = orgData.PhoneNumber,
                Website = orgData.Website,
                CompanyNumber = orgData.CompanyNumber,
                Approved = orgData.Approved,
                EventsCount = await _db.Events.CountAsync(e => e.OrganizerId == userId),
                PostsCount = await _db.Posts.CountAsync(p => p.OrganizerId == userId),
                TicketTypesCount = ticketTypesCount,
                TicketsSoldCount = ticketsSoldCount,
                EventsWithTicketsCount = eventsWithTickets,
                RecentEvents = recentEvents,
                RecentPosts = recentPosts,
                EventTicketRows = eventTicketRows,
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = _userManager.GetUserId(User)!;
            var orgData = await _db.OrganizerData.AsNoTracking().FirstOrDefaultAsync(o => o.OrganizerId == userId);

            var vm = orgData == null
                ? new OrganizerProfileViewModel { OrganizationName = string.Empty }
                : new OrganizerProfileViewModel
                {
                    OrganizationName = orgData.OrganizationName,
                    Description = orgData.Description,
                    PhoneNumber = orgData.PhoneNumber,
                    Website = orgData.Website,
                    CompanyNumber = orgData.CompanyNumber,
                    Approved = orgData.Approved,
                };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(OrganizerProfileViewModel input)
        {
            if (!ModelState.IsValid) return View(input);

            var userId = _userManager.GetUserId(User)!;
            var orgData = await _db.OrganizerData.FirstOrDefaultAsync(o => o.OrganizerId == userId);

            if (orgData == null)
            {
                _db.OrganizerData.Add(new OrganizerData
                {
                    OrganizerId = userId,
                    OrganizationName = input.OrganizationName,
                    Description = input.Description,
                    PhoneNumber = input.PhoneNumber,
                    Website = input.Website,
                    CompanyNumber = input.CompanyNumber,
                    Approved = false,
                });
            }
            else
            {
                orgData.OrganizationName = input.OrganizationName;
                orgData.Description = input.Description;
                orgData.PhoneNumber = input.PhoneNumber;
                orgData.Website = input.Website;
                orgData.CompanyNumber = input.CompanyNumber;
            }

            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = "Profile saved.";
            return RedirectToAction(nameof(Dashboard));
        }
    }
}
