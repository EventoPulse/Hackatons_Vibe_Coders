using EventsApp.Data;
using EventsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Controllers.Api
{
    [ApiController]
    [Route("api/messages")]
    [IgnoreAntiforgeryToken]
    [Authorize(Policy = "ApiAuth")]
    public class MessagesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessagesApiController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // GET /api/messages/conversations
        [HttpGet("conversations")]
        public async Task<IActionResult> Conversations()
        {
            var userId = _userManager.GetUserId(User)!;

            var convos = await _db.Conversations
                .AsNoTracking()
                .Where(c => c.ParticipantOneId == userId || c.ParticipantTwoId == userId)
                .Include(c => c.ParticipantOne)
                .Include(c => c.ParticipantTwo)
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();

            return Ok(convos.Select(c =>
            {
                var other = c.ParticipantOneId == userId ? c.ParticipantTwo : c.ParticipantOne;
                var last = c.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
                return new
                {
                    token = c.Token.ToString(),
                    otherUserId = other.Id,
                    otherUserName = other.UserName,
                    otherUserImageUrl = other.ProfileImageUrl,
                    lastMessage = last?.Content,
                    lastMessageAt = last?.CreatedAt,
                    unreadCount = c.Messages.Count(m => m.SenderId != userId && m.SeenAt == null && !m.IsDeleted),
                    status = c.Status.ToString(),
                };
            }));
        }

        [HttpPost("conversations")]
        public async Task<IActionResult> FindOrCreateConversation([FromBody] StartConversationDto dto)
        {
            var userId = _userManager.GetUserId(User)!;
            var otherUserId = (dto.UserId ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(otherUserId) || otherUserId == userId)
                return BadRequest(new { error = "Невалиден получател." });

            var otherExists = await _db.Users.AnyAsync(u => u.Id == otherUserId);
            if (!otherExists) return NotFound(new { error = "Потребителят не е намерен." });

            var convo = await _db.Conversations
                .FirstOrDefaultAsync(c =>
                    (c.ParticipantOneId == userId && c.ParticipantTwoId == otherUserId) ||
                    (c.ParticipantOneId == otherUserId && c.ParticipantTwoId == userId));

            if (convo == null)
            {
                convo = new Conversation
                {
                    ParticipantOneId = userId,
                    ParticipantTwoId = otherUserId,
                    Status = ConversationStatus.Pending,
                    RequestedByUserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };
                _db.Conversations.Add(convo);
                await _db.SaveChangesAsync();
            }

            return Ok(new { token = convo.Token.ToString() });
        }

        // GET /api/messages/conversations/{token}
        [HttpGet("conversations/{token}")]
        public async Task<IActionResult> ConversationDetails(Guid token)
        {
            var userId = _userManager.GetUserId(User)!;

            var convo = await _db.Conversations
                .Where(c => c.Token == token)
                .Include(c => c.ParticipantOne)
                .Include(c => c.ParticipantTwo)
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(50))
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync();

            if (convo == null) return NotFound();
            if (convo.ParticipantOneId != userId && convo.ParticipantTwoId != userId) return Forbid();

            foreach (var message in convo.Messages.Where(m => m.SenderId != userId && m.SeenAt == null))
            {
                message.SeenAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();

            var other = convo.ParticipantOneId == userId ? convo.ParticipantTwo : convo.ParticipantOne;

            return Ok(new
            {
                token = convo.Token.ToString(),
                otherUserId = other.Id,
                otherUserName = other.UserName,
                otherUserImageUrl = other.ProfileImageUrl,
                status = convo.Status.ToString(),
                messages = convo.Messages
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new
                    {
                        id = m.Id,
                        content = m.Content,
                        senderId = m.SenderId,
                        senderName = m.Sender?.UserName,
                        createdAt = m.CreatedAt,
                        editedAt = m.EditedAt,
                        isDeleted = m.IsDeleted,
                        canEdit = m.SenderId == userId && !m.IsDeleted,
                        canDelete = m.SenderId == userId && !m.IsDeleted,
                    }),
            });
        }

        [HttpPost("conversations/{token}/approve")]
        public async Task<IActionResult> Approve(Guid token)
        {
            return await SetStatus(token, ConversationStatus.Accepted);
        }

        [HttpPost("conversations/{token}/decline")]
        public async Task<IActionResult> Decline(Guid token)
        {
            return await SetStatus(token, ConversationStatus.Declined);
        }

        // POST /api/messages/conversations/{token}
        [HttpPost("conversations/{token}")]
        public async Task<IActionResult> SendMessage(Guid token, [FromBody] SendMessageDto dto)
        {
            var userId = _userManager.GetUserId(User)!;

            var convo = await _db.Conversations
                .Where(c => c.Token == token)
                .FirstOrDefaultAsync();

            if (convo == null) return NotFound();
            if (convo.ParticipantOneId != userId && convo.ParticipantTwoId != userId) return Forbid();
            if (convo.Status == ConversationStatus.Declined) return BadRequest(new { error = "Разговорът е отказан." });

            var message = new Message
            {
                ConversationId = convo.Id,
                SenderId = userId,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow,
            };

            _db.Messages.Add(message);
            convo.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                id = message.Id,
                content = message.Content,
                senderId = message.SenderId,
                senderName = (await _userManager.FindByIdAsync(userId))?.UserName,
                createdAt = message.CreatedAt,
            });
        }

        [HttpPut("messages/{id:int}")]
        public async Task<IActionResult> EditMessage(int id, [FromBody] SendMessageDto dto)
        {
            var userId = _userManager.GetUserId(User)!;
            var message = await _db.Messages.FirstOrDefaultAsync(m => m.Id == id);
            if (message == null) return NotFound();
            if (message.SenderId != userId) return Forbid();
            if (message.IsDeleted) return BadRequest(new { error = "Съобщението е изтрито." });
            if (string.IsNullOrWhiteSpace(dto.Content)) return BadRequest(new { error = "Съобщението не може да е празно." });

            message.Content = dto.Content.Trim();
            message.EditedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { id = message.Id, content = message.Content, editedAt = message.EditedAt });
        }

        [HttpDelete("messages/{id:int}")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var message = await _db.Messages.FirstOrDefaultAsync(m => m.Id == id);
            if (message == null) return NotFound();
            if (message.SenderId != userId) return Forbid();
            message.Content = string.Empty;
            message.IsDeleted = true;
            message.DeletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { deleted = true });
        }

        private async Task<IActionResult> SetStatus(Guid token, ConversationStatus status)
        {
            var userId = _userManager.GetUserId(User)!;
            var convo = await _db.Conversations.FirstOrDefaultAsync(c => c.Token == token);
            if (convo == null) return NotFound();
            if (convo.ParticipantOneId != userId && convo.ParticipantTwoId != userId) return Forbid();
            if (convo.RequestedByUserId == userId && !User.IsInRole("Admin")) return Forbid();
            convo.Status = status;
            convo.RespondedAt = DateTime.UtcNow;
            convo.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { status = convo.Status.ToString() });
        }
    }

    public record StartConversationDto(string? UserId);
    public record SendMessageDto(string Content);
}
