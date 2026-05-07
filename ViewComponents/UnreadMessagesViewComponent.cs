using EventsApp.Data;
using EventsApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.ViewComponents
{
    public class UnreadMessagesViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public UnreadMessagesViewComponent(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!(User.Identity?.IsAuthenticated ?? false))
            {
                return View(0);
            }

            var userId = _userManager.GetUserId((System.Security.Claims.ClaimsPrincipal)User);
            if (string.IsNullOrEmpty(userId))
            {
                return View(0);
            }

            var count = await _db.Messages
                .AsNoTracking()
                .Where(m => m.SenderId != userId
                    && m.SeenAt == null
                    && (m.Conversation.ParticipantOneId == userId || m.Conversation.ParticipantTwoId == userId))
                .CountAsync();

            return View(count);
        }
    }
}
