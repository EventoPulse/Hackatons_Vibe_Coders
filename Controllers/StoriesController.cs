using EventsApp.Data;
using EventsApp.Models;
using EventsApp.Services;
using EventsApp.ViewModels.Social;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Controllers
{
    [Authorize]
    public class StoriesController : Controller
    {
        private const long UploadSizeLimit = 100L * 1024 * 1024;

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMediaUploadService _mediaUpload;
        private readonly IPlatformPermissionService _permissions;
        private readonly IBusinessContextService _businessContext;

        public StoriesController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IMediaUploadService mediaUpload,
            IPlatformPermissionService permissions,
            IBusinessContextService businessContext)
        {
            _db = db;
            _userManager = userManager;
            _mediaUpload = mediaUpload;
            _permissions = permissions;
            _businessContext = businessContext;
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!await _permissions.CanCreateStoryAsync(User))
            {
                TempData["StatusMessage"] = "Only approved organizers can publish stories.";
                return Forbid();
            }

            var context = await _businessContext.GetContextAsync(HttpContext);
            if (context.Page == null || context.Workspace == null)
            {
                TempData["StatusMessage"] = "Избери активна organizer page преди да публикуваш story.";
                return RedirectToAction("Dashboard", "Organizer");
            }

            return View(new StoryCreateViewModel
            {
                ActiveWorkspaceName = context.Workspace.DisplayName,
                ActivePageName = context.Page.DisplayName,
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(UploadSizeLimit)]
        public async Task<IActionResult> Create(StoryCreateViewModel input, string? returnUrl)
        {
            if (!await _permissions.CanCreateStoryAsync(User))
            {
                TempData["StatusMessage"] = "Only approved organizers can publish stories.";
                return Forbid();
            }

            var context = await _businessContext.GetContextAsync(HttpContext);
            if (context.Page == null || context.Workspace == null)
            {
                ModelState.AddModelError(string.Empty, "Избери активна organizer page преди да публикуваш story.");
            }
            else if (!await _permissions.CanPublishAsIdentityAsync(User, AuthorIdentityType.OrganizerPage, context.Page.Id))
            {
                return Forbid();
            }

            MediaUploadResult? media = null;

            if (input.MediaFile != null && input.MediaFile.Length > 0)
            {
                try
                {
                    media = await _mediaUpload.SaveAsync(input.MediaFile, "stories");
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(nameof(input.MediaFile), ex.Message);
                }
            }
            else
            {
                ModelState.AddModelError(nameof(input.MediaFile), "Upload an image or video file.");
            }

            if (!ModelState.IsValid || media == null)
            {
                input.ActiveWorkspaceName = context.Workspace?.DisplayName;
                input.ActivePageName = context.Page?.DisplayName;
                return View(input);
            }

            var now = DateTime.UtcNow;
            _db.Stories.Add(new Story
            {
                AuthorId = _userManager.GetUserId(User)!,
                OrganizerProfileId = context.Page!.Id,
                BusinessWorkspaceId = context.Workspace!.Id,
                MediaUrl = media.Url,
                MediaType = media.MediaType,
                Caption = string.IsNullOrWhiteSpace(input.Caption) ? null : input.Caption.Trim(),
                CreatedAt = now,
                ExpiresAt = now.AddHours(24),
            });

            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = "Story published for 24 hours.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Posts");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? returnUrl)
        {
            var userId = _userManager.GetUserId(User)!;
            var story = await _db.Stories.FirstOrDefaultAsync(s => s.Id == id);
            if (story == null)
            {
                return NotFound();
            }

            if (story.AuthorId != userId && !User.IsInRole(Common.GlobalConstants.Roles.Admin))
            {
                return Forbid();
            }

            _db.Stories.Remove(story);
            await _db.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Posts");
        }

        private static bool LooksLikeVideoUrl(string url)
        {
            var path = url.Split('?', '#')[0];
            var ext = Path.GetExtension(path);
            return ext.Equals(".mp4", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".webm", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".mov", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".m4v", StringComparison.OrdinalIgnoreCase);
        }
    }
}
