using EventsApp.Common;
using EventsApp.Data;
using EventsApp.Models;
using EventsApp.ViewModels.Account;
using EventsApp.ViewModels.Events;
using EventsApp.ViewModels.Posts;
using EventsApp.ViewModels.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? GlobalConstants.Roles.User;

            var orgData = await _db.OrganizerData
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrganizerId == user.Id);

            var preferences = await _db.UserPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            var vm = new AccountOverviewViewModel
            {
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Bio = user.Bio,
                ProfileImageUrl = user.ProfileImageUrl,
                CreatedAt = user.CreatedAt,
                Role = role,
                HasApplied = orgData != null,
                IsApproved = orgData?.Approved ?? false,
                OrganizationName = orgData?.OrganizationName,
                ApplicationDate = orgData?.CreatedAt,
                HasPreferences = preferences != null,
                PreferredGenre = preferences?.PreferredGenre,
                PreferredCity = preferences?.PreferredCity,
                MinAge = preferences?.MinAge,
                MaxDistanceKm = preferences?.MaxDistanceKm,
            };

            if (role == GlobalConstants.Roles.Organizer)
            {
                vm.EventsCount = await _db.Events.CountAsync(e => e.OrganizerId == user.Id);
                vm.PostsCount = await _db.Posts.CountAsync(p => p.OrganizerId == user.Id);
            }

            vm.LikedEvents = await _db.EventLikes
                .AsNoTracking()
                .Where(l => l.UserId == user.Id &&
                    (l.Event.IsApproved || role == GlobalConstants.Roles.Admin || l.Event.OrganizerId == user.Id))
                .OrderByDescending(l => l.CreatedAt)
                .Take(4)
                .Select(l => new EventCardViewModel
                {
                    Id = l.EventId,
                    Title = l.Event.Title,
                    ImageUrl = l.Event.ImageUrl,
                    Address = l.Event.Address,
                    City = l.Event.City,
                    StartTime = l.Event.StartTime,
                    Genre = l.Event.Genre,
                    IsApproved = l.Event.IsApproved,
                    OrganizerId = l.Event.OrganizerId,
                    OrganizerName = l.Event.Organizer.UserName ?? string.Empty,
                    LikesCount = l.Event.Likes.Count,
                    CommentsCount = l.Event.Comments.Count,
                    CurrentUserLiked = true,
                })
                .ToListAsync();

            vm.LikedPosts = await _db.PostLikes
                .AsNoTracking()
                .Where(l => l.UserId == user.Id)
                .OrderByDescending(l => l.CreatedAt)
                .Take(4)
                .Select(l => new PostCardViewModel
                {
                    Id = l.PostId,
                    OrganizerId = l.Post.OrganizerId,
                    OrganizerName = l.Post.Organizer.UserName ?? string.Empty,
                    Content = l.Post.Content,
                    CreatedAt = l.Post.CreatedAt,
                    EventId = l.Post.EventId,
                    EventTitle = l.Post.Event != null ? l.Post.Event.Title : null,
                    FirstMediaUrl = l.Post.Images.Select(i => i.ImageUrl).FirstOrDefault(),
                    FirstMediaType = l.Post.Images.Select(i => i.MediaType).FirstOrDefault(),
                    LikesCount = l.Post.Likes.Count,
                    CommentsCount = l.Post.Comments.Count,
                    CurrentUserLiked = true,
                })
                .ToListAsync();

            vm.PurchasedTicketsCount = await _db.UserTickets
                .CountAsync(ut => ut.Transaction.UserId == user.Id);

            vm.RecentTickets = await _db.UserTickets
                .AsNoTracking()
                .Where(ut => ut.Transaction.UserId == user.Id)
                .OrderByDescending(ut => ut.CreatedAt)
                .Take(5)
                .Select(ut => new MyTicketRowViewModel
                {
                    Id = ut.Id,
                    EventId = ut.Ticket.EventId,
                    EventTitle = ut.Ticket.Event.Title,
                    TicketName = ut.Ticket.Name,
                    Address = ut.Ticket.Event.Address,
                    City = ut.Ticket.Event.City,
                    StartTime = ut.Ticket.Event.StartTime,
                    Price = ut.Ticket.Price,
                    IsUsed = ut.IsUsed,
                    CreatedAt = ut.CreatedAt,
                })
                .ToListAsync();

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            return View(CreateEditProfileViewModel(user));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel input)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            input.UserName = input.UserName?.Trim() ?? string.Empty;
            input.PhoneNumber = string.IsNullOrWhiteSpace(input.PhoneNumber) ? null : input.PhoneNumber.Trim();
            input.Bio = string.IsNullOrWhiteSpace(input.Bio) ? null : input.Bio.Trim();
            input.ProfileImageUrl = string.IsNullOrWhiteSpace(input.ProfileImageUrl) ? null : input.ProfileImageUrl.Trim();
            input.Email = user.Email;
            input.CreatedAt = user.CreatedAt;

            ModelState.Clear();
            if (!TryValidateModel(input))
            {
                return View(input);
            }

            user.UserName = input.UserName;
            user.PhoneNumber = input.PhoneNumber;
            user.Bio = input.Bio;
            user.ProfileImageUrl = input.ProfileImageUrl;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                AddIdentityErrors(result);
                return View(input);
            }

            TempData["StatusMessage"] = "Profile updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Apply()
        {
            var userId = _userManager.GetUserId(User)!;

            if (await _db.OrganizerData.AnyAsync(o => o.OrganizerId == userId))
            {
                TempData["StatusMessage"] = "You have already submitted an application.";
                return RedirectToAction(nameof(Index));
            }

            return View(new ApplyOrganizerViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(ApplyOrganizerViewModel input)
        {
            if (!ModelState.IsValid) return View(input);

            var userId = _userManager.GetUserId(User)!;

            if (await _db.OrganizerData.AnyAsync(o => o.OrganizerId == userId))
            {
                TempData["StatusMessage"] = "You have already submitted an application.";
                return RedirectToAction(nameof(Index));
            }

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

            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = "Application submitted! An admin will review it shortly.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> EditApplication()
        {
            var userId = _userManager.GetUserId(User)!;
            var orgData = await _db.OrganizerData.AsNoTracking().FirstOrDefaultAsync(o => o.OrganizerId == userId);
            if (orgData == null) return RedirectToAction(nameof(Apply));

            if (orgData.Approved && User.IsInRole(GlobalConstants.Roles.Organizer))
            {
                return RedirectToAction("Profile", "Organizer");
            }

            return View(new ApplyOrganizerViewModel
            {
                OrganizationName = orgData.OrganizationName,
                Description = orgData.Description,
                PhoneNumber = orgData.PhoneNumber,
                Website = orgData.Website,
                CompanyNumber = orgData.CompanyNumber,
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditApplication(ApplyOrganizerViewModel input)
        {
            if (!ModelState.IsValid) return View(input);

            var userId = _userManager.GetUserId(User)!;
            var orgData = await _db.OrganizerData.FirstOrDefaultAsync(o => o.OrganizerId == userId);
            if (orgData == null) return RedirectToAction(nameof(Apply));

            orgData.OrganizationName = input.OrganizationName;
            orgData.Description = input.Description;
            orgData.PhoneNumber = input.PhoneNumber;
            orgData.Website = input.Website;
            orgData.CompanyNumber = input.CompanyNumber;

            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = "Application updated.";
            return RedirectToAction(nameof(Index));
        }

        private static EditProfileViewModel CreateEditProfileViewModel(ApplicationUser user)
        {
            return new EditProfileViewModel
            {
                UserName = user.UserName ?? string.Empty,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Bio = user.Bio,
                ProfileImageUrl = user.ProfileImageUrl,
                CreatedAt = user.CreatedAt,
            };
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}
