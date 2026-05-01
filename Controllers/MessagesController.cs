using EventsApp.Common;
using EventsApp.Data;
using EventsApp.Models;
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

        public MessagesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
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
                        SenderName = m.SenderId == userId ? "You" : GetDisplayName(m.Sender),
                        Content = m.Content,
                        CreatedAt = m.CreatedAt,
                        SeenAt = m.SeenAt,
                        IsMine = m.SenderId == userId,
                    })
                    .ToList(),
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
        public async Task<IActionResult> Send(int id, string content)
        {
            var userId = _userManager.GetUserId(User)!;
            var conversation = await _db.Conversations
                .FirstOrDefaultAsync(c => c.Id == id && (c.ParticipantOneId == userId || c.ParticipantTwoId == userId));

            if (conversation == null)
            {
                return NotFound();
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
            _db.Messages.Add(new Message
            {
                ConversationId = id,
                SenderId = userId,
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
    }
}
