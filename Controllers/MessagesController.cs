using EventsApp.Common;
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
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPlatformPermissionService _permissions;
        private readonly IActingIdentityService _actingIdentity;

        public MessagesController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IPlatformPermissionService permissions,
            IActingIdentityService actingIdentity)
        {
            _db = db;
            _userManager = userManager;
            _permissions = permissions;
            _actingIdentity = actingIdentity;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;

            var conversations = await _db.Conversations
                .AsNoTracking()
                .Where(c => c.ParticipantOneId == userId || c.ParticipantTwoId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .Select(c => new
                {
                    c.Id,
                    c.UpdatedAt,
                    Other = c.ParticipantOneId == userId ? c.ParticipantTwo : c.ParticipantOne,
                    LastMessage = c.Messages
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => m.Content)
                        .FirstOrDefault(),
                    UnseenCount = c.Messages.Count(m => m.SenderId != userId && m.SeenAt == null),
                })
                .Select(c => new ConversationListItemViewModel
                {
                    Id = c.Id,
                    OtherUserId = c.Other.Id,
                    OtherUserName = c.Other.OrganizerData != null && c.Other.OrganizerData.Approved
                        ? c.Other.OrganizerData.OrganizationName
                        : c.Other.UserName ?? string.Empty,
                    OtherUserImageUrl = c.Other.ProfileImageUrl,
                    LastMessage = c.LastMessage,
                    UpdatedAt = c.UpdatedAt,
                    UnseenCount = c.UnseenCount,
                })
                .ToListAsync();

            return View(conversations);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var conversation = await _db.Conversations
                .Include(c => c.ParticipantOne)
                    .ThenInclude(u => u.OrganizerData)
                .Include(c => c.ParticipantTwo)
                    .ThenInclude(u => u.OrganizerData)
                .Include(c => c.Messages)
                    .ThenInclude(m => m.Sender)
                .Include(c => c.Messages)
                    .ThenInclude(m => m.AuthorOrganizerProfile)
                .FirstOrDefaultAsync(c => c.Id == id && (c.ParticipantOneId == userId || c.ParticipantTwoId == userId));

            if (conversation == null)
            {
                return NotFound();
            }

            var now = DateTime.UtcNow;
            var unseen = conversation.Messages.Where(m => m.SenderId != userId && m.SeenAt == null).ToList();
            foreach (var message in unseen)
            {
                message.SeenAt = now;
            }

            if (unseen.Count > 0)
            {
                await _db.SaveChangesAsync();
            }

            var other = conversation.ParticipantOneId == userId
                ? conversation.ParticipantTwo
                : conversation.ParticipantOne;

            var vm = new ConversationDetailsViewModel
            {
                Id = conversation.Id,
                OtherUserId = other.Id,
                OtherUserName = GetDisplayName(other),
                OtherUserImageUrl = other.ProfileImageUrl,
                Messages = conversation.Messages
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new MessageBubbleViewModel
                    {
                        Id = m.Id,
                        SenderId = m.SenderId,
                        SenderName = GetMessageDisplayName(m, userId),
                        SenderImageUrl = m.AuthorType == AuthorIdentityType.OrganizerPage ? m.AuthorOrganizerProfile?.AvatarImageUrl : m.Sender.ProfileImageUrl,
                        SenderBadgeKey = GetAuthorBadgeKey(m.AuthorType),
                        SenderBadgeText = GetAuthorBadgeText(m.AuthorType, m.SenderId == userId),
                        Content = m.Content,
                        CreatedAt = m.CreatedAt,
                        SeenAt = m.SeenAt,
                        IsMine = m.SenderId == userId,
                    })
                    .ToList(),
                ActingIdentities = await _actingIdentity.GetOptionsAsync(HttpContext, null),
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(string userId)
        {
            var currentUserId = _userManager.GetUserId(User)!;
            if (string.IsNullOrWhiteSpace(userId) || userId == currentUserId)
            {
                return RedirectToAction("Index", "Posts");
            }

            if (!await _db.Users.AnyAsync(u => u.Id == userId))
            {
                return NotFound();
            }

            if (!await _permissions.CanMessageUserAsync(User, userId))
            {
                TempData["StatusMessage"] = "Messaging is limited to organizer questions, organizer replies, or mutual follows.";
                return Forbid();
            }

            var (one, two) = SortParticipants(currentUserId, userId);
            var conversation = await _db.Conversations
                .FirstOrDefaultAsync(c => c.ParticipantOneId == one && c.ParticipantTwoId == two);

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    ParticipantOneId = one,
                    ParticipantTwoId = two,
                };
                _db.Conversations.Add(conversation);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = conversation.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int id, string content, string? actingIdentityKey)
        {
            var userId = _userManager.GetUserId(User)!;
            var conversation = await _db.Conversations
                .FirstOrDefaultAsync(c => c.Id == id && (c.ParticipantOneId == userId || c.ParticipantTwoId == userId));

            if (conversation == null)
            {
                return NotFound();
            }

            var otherUserId = conversation.ParticipantOneId == userId
                ? conversation.ParticipantTwoId
                : conversation.ParticipantOneId;

            if (!await _permissions.CanMessageUserAsync(User, otherUserId))
            {
                TempData["StatusMessage"] = "Messaging is limited to organizer questions, organizer replies, or mutual follows.";
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["StatusMessage"] = "Message cannot be empty.";
                return RedirectToAction(nameof(Details), new { id });
            }

            content = content.Trim();
            if (content.Length > GlobalConstants.Social.MessageContentMaxLength)
            {
                content = content[..GlobalConstants.Social.MessageContentMaxLength];
            }

            var now = DateTime.UtcNow;
            var identity = await _actingIdentity.ResolveAsync(HttpContext, actingIdentityKey, null, includePersonal: !User.IsInRole(GlobalConstants.Roles.Organizer));
            if (identity == null || !await _permissions.CanMessageAsIdentityAsync(User, id, identity.Type, identity.OrganizerProfileId))
            {
                return Forbid();
            }

            _db.Messages.Add(new Message
            {
                ConversationId = id,
                SenderId = userId,
                AuthorType = identity.Type,
                AuthorOrganizerProfileId = identity.OrganizerProfileId,
                BusinessWorkspaceId = identity.BusinessWorkspaceId,
                Content = content,
                CreatedAt = now,
            });

            conversation.UpdatedAt = now;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        private static (string One, string Two) SortParticipants(string first, string second)
        {
            return string.CompareOrdinal(first, second) <= 0 ? (first, second) : (second, first);
        }

        private static string GetDisplayName(ApplicationUser user)
        {
            if (user.OrganizerData?.Approved == true && !string.IsNullOrWhiteSpace(user.OrganizerData.OrganizationName))
            {
                return user.OrganizerData.OrganizationName;
            }

            var name = string.Join(" ", new[] { user.FirstName, user.LastName }.Where(v => !string.IsNullOrWhiteSpace(v)));
            return string.IsNullOrWhiteSpace(name) ? user.UserName ?? string.Empty : name;
        }

        private static string GetMessageDisplayName(Message message, string currentUserId)
        {
            if (message.AuthorType == AuthorIdentityType.OrganizerPage && message.AuthorOrganizerProfile != null)
            {
                return message.AuthorOrganizerProfile.DisplayName;
            }

            return message.SenderId == currentUserId ? "You" : GetDisplayName(message.Sender);
        }

        private static string GetAuthorBadgeKey(AuthorIdentityType type)
        {
            return type switch
            {
                AuthorIdentityType.OrganizerPage => "identity.page",
                AuthorIdentityType.Admin => "identity.admin",
                AuthorIdentityType.System => "identity.system",
                _ => "identity.user",
            };
        }

        private static string GetAuthorBadgeText(AuthorIdentityType type, bool isYou)
        {
            if (isYou)
            {
                return "You";
            }

            return type switch
            {
                AuthorIdentityType.OrganizerPage => "Organizer Page",
                AuthorIdentityType.Admin => "Admin",
                AuthorIdentityType.System => "System",
                _ => "User",
            };
        }
    }
}
