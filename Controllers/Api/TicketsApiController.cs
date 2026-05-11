using EventsApp.Data;
using EventsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Controllers.Api
{
    [ApiController]
    [Route("api/tickets")]
    [IgnoreAntiforgeryToken]
    [Authorize(Policy = "ApiAuth")]
    public class TicketsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public TicketsApiController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // GET /api/tickets/mine
        [HttpGet("mine")]
        public async Task<IActionResult> Mine()
        {
            var userId = _userManager.GetUserId(User)!;

            var tickets = await _db.UserTickets
                .AsNoTracking()
                .Where(ut => ut.Transaction.UserId == userId)
                .Include(ut => ut.Ticket)
                    .ThenInclude(t => t.Event)
                .OrderByDescending(ut => ut.CreatedAt)
                .ToListAsync();

            return Ok(tickets.Select(ut => new
            {
                id = ut.Id,
                eventId = ut.Ticket.EventId,
                eventTitle = ut.Ticket.Event.Title,
                eventStartTime = ut.Ticket.Event.StartTime,
                eventAddress = ut.Ticket.Event.Address,
                eventCity = ut.Ticket.Event.City,
                ticketType = ut.Ticket.Name,
                qrCodeUrl = $"/api/tickets/{ut.Id}/qr",
                isUsed = ut.IsUsed,
                purchasedAt = ut.CreatedAt,
            }));
        }

        // GET /api/tickets/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Details(Guid id)
        {
            var userId = _userManager.GetUserId(User)!;

            var ut = await _db.UserTickets
                .AsNoTracking()
                .Where(t => t.Id == id)
                .Include(t => t.Ticket)
                    .ThenInclude(t => t.Event)
                .Include(t => t.Transaction)
                .FirstOrDefaultAsync();

            if (ut == null) return NotFound();
            if (ut.Transaction.UserId != userId && !User.IsInRole("Admin")) return Forbid();

            return Ok(new
            {
                id = ut.Id,
                eventId = ut.Ticket.EventId,
                eventTitle = ut.Ticket.Event.Title,
                eventStartTime = ut.Ticket.Event.StartTime,
                eventAddress = ut.Ticket.Event.Address,
                eventCity = ut.Ticket.Event.City,
                ticketType = ut.Ticket.Name,
                qrCodeUrl = $"/api/tickets/{ut.Id}/qr",
                isUsed = ut.IsUsed,
                purchasedAt = ut.CreatedAt,
            });
        }
    }
}
