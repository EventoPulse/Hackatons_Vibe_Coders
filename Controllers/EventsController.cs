using EventsApp.Common;
using EventsApp.Data;
using EventsApp.Models;
using EventsApp.ViewModels.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public EventsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? search, string? city, EventGenre? genre, DateTime? dateFrom)
        {
            var isAdmin = User.IsInRole(GlobalConstants.Roles.Admin);

            var query = _db.Events.AsNoTracking().AsQueryable();
            if (!isAdmin)
            {
                query = query.Where(e => e.IsApproved);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e => e.Title.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(e => e.Venue.City == city);
            }

            if (genre.HasValue)
            {
                query = query.Where(e => e.Genre == genre.Value);
            }

            if (dateFrom.HasValue)
            {
                var from = dateFrom.Value.Date;
                query = query.Where(e => e.StartTime >= from);
            }

            var events = await query
                .OrderBy(e => e.StartTime)
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

            var cities = await _db.Venues
                .AsNoTracking()
                .Select(v => v.City)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return View(new EventsIndexViewModel
            {
                Search = search,
                City = city,
                Genre = genre,
                DateFrom = dateFrom,
                Events = events,
                Cities = cities,
            });
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole(GlobalConstants.Roles.Admin);

            var ev = await _db.Events
                .AsNoTracking()
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .Include(e => e.Images)
                .Include(e => e.Likes)
                .Include(e => e.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null)
            {
                return NotFound();
            }

            if (!ev.IsApproved && !isAdmin && ev.OrganizerId != userId)
            {
                return NotFound();
            }

            var vm = new EventDetailsViewModel
            {
                Id = ev.Id,
                Title = ev.Title,
                Description = ev.Description,
                StartTime = ev.StartTime,
                EndTime = ev.EndTime,
                Genre = ev.Genre,
                ImageUrl = ev.ImageUrl,
                IsApproved = ev.IsApproved,
                VenueId = ev.VenueId,
                VenueName = ev.Venue.Name,
                VenueCity = ev.Venue.City,
                VenueAddress = ev.Venue.Address,
                OrganizerId = ev.OrganizerId,
                OrganizerName = ev.Organizer.UserName ?? string.Empty,
                ImageUrls = ev.Images.Select(i => i.ImageUrl).ToList(),
                LikesCount = ev.Likes.Count,
                CurrentUserLiked = userId != null && ev.Likes.Any(l => l.UserId == userId),
                Comments = ev.Comments
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new EventCommentViewModel
                    {
                        Id = c.Id,
                        UserId = c.UserId,
                        UserName = c.User.UserName ?? string.Empty,
                        Content = c.Content,
                        CreatedAt = c.CreatedAt,
                        CanDelete = isAdmin || c.UserId == userId,
                    })
                    .ToList(),
                CanEdit = isAdmin || ev.OrganizerId == userId,
                CanDelete = isAdmin || ev.OrganizerId == userId,
            };

            return View(vm);
        }

        [Authorize(Roles = GlobalConstants.Roles.Admin + "," + GlobalConstants.Roles.Organizer)]
        public async Task<IActionResult> Create()
        {
            var vm = new EventCreateEditViewModel
            {
                Venues = await GetVenueOptionsAsync(),
                CanEditApproval = User.IsInRole(GlobalConstants.Roles.Admin),
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = GlobalConstants.Roles.Admin + "," + GlobalConstants.Roles.Organizer)]
        public async Task<IActionResult> Create(EventCreateEditViewModel input)
        {
            var userId = _userManager.GetUserId(User)!;
            var isAdmin = User.IsInRole(GlobalConstants.Roles.Admin);

            if (input.EndTime <= input.StartTime)
            {
                ModelState.AddModelError(nameof(input.EndTime), "End time must be after start time.");
            }

            var venue = await _db.Venues.FirstOrDefaultAsync(v => v.Id == input.VenueId);
            if (venue == null)
            {
                ModelState.AddModelError(nameof(input.VenueId), "Select a valid venue.");
            }
            else if (!isAdmin && venue.OrganizerId != userId)
            {
                ModelState.AddModelError(nameof(input.VenueId), "You can only use your own venues.");
            }

            if (!ModelState.IsValid)
            {
                input.Venues = await GetVenueOptionsAsync();
                input.CanEditApproval = isAdmin;
                return View(input);
            }

            var ev = new Event
            {
                Title = input.Title,
                Description = input.Description,
                VenueId = input.VenueId,
                OrganizerId = userId,
                StartTime = input.StartTime,
                EndTime = input.EndTime,
                Genre = input.Genre,
                ImageUrl = input.ImageUrl,
                IsApproved = isAdmin && input.IsApproved,
            };

            _db.Events.Add(ev);
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = "Event created.";
            return RedirectToAction(nameof(Details), new { id = ev.Id });
        }

        [Authorize(Roles = GlobalConstants.Roles.Admin + "," + GlobalConstants.Roles.Organizer)]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var isAdmin = User.IsInRole(GlobalConstants.Roles.Admin);

            var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null)
            {
                return NotFound();
            }

            if (!isAdmin && ev.OrganizerId != userId)
            {
                return Forbid();
            }

            var vm = new EventCreateEditViewModel
            {
                Id = ev.Id,
                Title = ev.Title,
                Description = ev.Description,
                VenueId = ev.VenueId,
                StartTime = ev.StartTime,
                EndTime = ev.EndTime,
                Genre = ev.Genre,
                ImageUrl = ev.ImageUrl,
                IsApproved = ev.IsApproved,
                CanEditApproval = isAdmin,
                Venues = await GetVenueOptionsAsync(),
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = GlobalConstants.Roles.Admin + "," + GlobalConstants.Roles.Organizer)]
        public async Task<IActionResult> Edit(int id, EventCreateEditViewModel input)
        {
            var userId = _userManager.GetUserId(User)!;
            var isAdmin = User.IsInRole(GlobalConstants.Roles.Admin);

            var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null)
            {
                return NotFound();
            }

            if (!isAdmin && ev.OrganizerId != userId)
            {
                return Forbid();
            }

            if (input.EndTime <= input.StartTime)
            {
                ModelState.AddModelError(nameof(input.EndTime), "End time must be after start time.");
            }

            var venue = await _db.Venues.FirstOrDefaultAsync(v => v.Id == input.VenueId);
            if (venue == null)
            {
                ModelState.AddModelError(nameof(input.VenueId), "Select a valid venue.");
            }
            else if (!isAdmin && venue.OrganizerId != userId)
            {
                ModelState.AddModelError(nameof(input.VenueId), "You can only use your own venues.");
            }

            if (!ModelState.IsValid)
            {
                input.Venues = await GetVenueOptionsAsync();
                input.CanEditApproval = isAdmin;
                return View(input);
            }

            ev.Title = input.Title;
            ev.Description = input.Description;
            ev.VenueId = input.VenueId;
            ev.StartTime = input.StartTime;
            ev.EndTime = input.EndTime;
            ev.Genre = input.Genre;
            ev.ImageUrl = input.ImageUrl;
            if (isAdmin)
            {
                ev.IsApproved = input.IsApproved;
            }

            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = "Event updated.";
            return RedirectToAction(nameof(Details), new { id = ev.Id });
        }

        [Authorize(Roles = GlobalConstants.Roles.Admin + "," + GlobalConstants.Roles.Organizer)]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var isAdmin = User.IsInRole(GlobalConstants.Roles.Admin);

            var ev = await _db.Events
                .AsNoTracking()
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null)
            {
                return NotFound();
            }

            if (!isAdmin && ev.OrganizerId != userId)
            {
                return Forbid();
            }

            return View(ev);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = GlobalConstants.Roles.Admin + "," + GlobalConstants.Roles.Organizer)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var isAdmin = User.IsInRole(GlobalConstants.Roles.Admin);

            var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null)
            {
                return NotFound();
            }

            if (!isAdmin && ev.OrganizerId != userId)
            {
                return Forbid();
            }

            _db.Events.Remove(ev);
            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = "Event deleted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Like(int id, string? returnUrl)
        {
            var userId = _userManager.GetUserId(User)!;
            var exists = await _db.EventLikes.AnyAsync(l => l.EventId == id && l.UserId == userId);
            if (!exists && await _db.Events.AnyAsync(e => e.Id == id))
            {
                _db.EventLikes.Add(new EventLike { EventId = id, UserId = userId });
                await _db.SaveChangesAsync();
            }
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Unlike(int id, string? returnUrl)
        {
            var userId = _userManager.GetUserId(User)!;
            var like = await _db.EventLikes.FirstOrDefaultAsync(l => l.EventId == id && l.UserId == userId);
            if (like != null)
            {
                _db.EventLikes.Remove(like);
                await _db.SaveChangesAsync();
            }
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AddComment(int id, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["StatusMessage"] = "Comment cannot be empty.";
                return RedirectToAction(nameof(Details), new { id });
            }

            content = content.Trim();
            if (content.Length > GlobalConstants.Comment.ContentMaxLength)
            {
                content = content[..GlobalConstants.Comment.ContentMaxLength];
            }

            if (!await _db.Events.AnyAsync(e => e.Id == id))
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User)!;
            _db.EventComments.Add(new EventComment
            {
                EventId = id,
                UserId = userId,
                Content = content,
            });
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var userId = _userManager.GetUserId(User)!;
            var isAdmin = User.IsInRole(GlobalConstants.Roles.Admin);

            var comment = await _db.EventComments.FirstOrDefaultAsync(c => c.Id == commentId);
            if (comment == null)
            {
                return NotFound();
            }

            if (!isAdmin && comment.UserId != userId)
            {
                return Forbid();
            }

            var eventId = comment.EventId;
            _db.EventComments.Remove(comment);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        [Authorize]
        public async Task<IActionResult> Recommended()
        {
            var userId = _userManager.GetUserId(User)!;
            var prefs = await _db.UserPreferences.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId);

            var now = DateTime.UtcNow;
            var query = _db.Events
                .AsNoTracking()
                .Where(e => e.IsApproved && e.StartTime >= now);

            if (prefs != null)
            {
                if (prefs.PreferredGenre.HasValue)
                {
                    query = query.Where(e => e.Genre == prefs.PreferredGenre.Value);
                }

                if (!string.IsNullOrWhiteSpace(prefs.PreferredCity))
                {
                    query = query.Where(e => e.Venue.City == prefs.PreferredCity);
                }
            }

            var events = await query
                .OrderBy(e => e.StartTime)
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

            ViewBag.HasPreferences = prefs != null;
            return View(events);
        }

        private async Task<IEnumerable<SelectListItem>> GetVenueOptionsAsync()
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole(GlobalConstants.Roles.Admin);

            var venues = _db.Venues.AsNoTracking();
            if (!isAdmin && userId != null)
            {
                venues = venues.Where(v => v.OrganizerId == userId);
            }

            return await venues
                .OrderBy(v => v.Name)
                .Select(v => new SelectListItem
                {
                    Value = v.Id.ToString(),
                    Text = v.Name + " — " + v.City,
                })
                .ToListAsync();
        }
    }
}
