using EventsApp.Common;
using EventsApp.Data;
using EventsApp.Models;
using EventsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Controllers.Api
{
    [ApiController]
    [Route("api/seat-inventory")]
    public class SeatInventoryApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISeatReservationService _seatReservations;

        public SeatInventoryApiController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            ISeatReservationService seatReservations)
        {
            _db = db;
            _userManager = userManager;
            _seatReservations = seatReservations;
        }

        [HttpGet("event/{eventId:int}")]
        public async Task<IActionResult> ForEvent(int eventId, int? occurrenceId)
        {
            await _seatReservations.ReleaseExpiredReservationsAsync();

            var query = _db.EventSeatInventories
                .AsNoTracking()
                .Include(i => i.Seat)
                    .ThenInclude(s => s.Section)
                .Where(i => occurrenceId.HasValue
                    ? i.EventOccurrenceId == occurrenceId.Value
                    : i.EventId == eventId && i.EventOccurrenceId == null);

            var seats = await query
                .Select(i => new
                {
                    i.Id,
                    i.SeatId,
                    Label = string.IsNullOrWhiteSpace(i.Seat.Label) ? i.Seat.Row + i.Seat.Number : i.Seat.Label,
                    i.Seat.Capacity,
                    i.Seat.IsCapacityUnlimited,
                    Type = i.Seat.SeatType.ToString(),
                    Section = i.Seat.Section.Name,
                    i.Seat.Section.PriceModifier,
                    Status = i.Status.ToString(),
                    i.ReservedUntil,
                })
                .ToListAsync();

            return Ok(seats);
        }

        [HttpPost("reserve")]
        [Authorize]
        public async Task<IActionResult> Reserve(SeatReservationRequest request)
        {
            var userId = _userManager.GetUserId(User)!;
            var reserved = await _seatReservations.ReserveSeatAsync(
                request.EventId,
                request.EventOccurrenceId,
                request.SeatId,
                userId,
                TimeSpan.FromMinutes(10));

            return reserved == null
                ? Conflict(new { ok = false, message = "Seat is not available." })
                : Ok(new { ok = true, reserved.Id, reserved.ReservedUntil });
        }

        [HttpPost("release")]
        [Authorize]
        public async Task<IActionResult> Release(SeatReleaseRequest request)
        {
            var ok = await _seatReservations.ReleaseReservationAsync(
                request.InventoryId,
                _userManager.GetUserId(User)!,
                User.IsInRole(GlobalConstants.Roles.Admin));

            return ok ? Ok(new { ok = true }) : NotFound(new { ok = false });
        }

        [HttpPost("status")]
        [Authorize(Roles = GlobalConstants.Roles.Admin + "," + GlobalConstants.Roles.Organizer)]
        public async Task<IActionResult> SetStatus(SeatStatusRequest request)
        {
            if (request.Status != EventSeatInventoryStatus.Available &&
                request.Status != EventSeatInventoryStatus.Blocked)
            {
                return BadRequest(new { ok = false, message = "Only Available and Blocked can be set manually." });
            }

            var userId = _userManager.GetUserId(User)!;
            var isAdmin = User.IsInRole(GlobalConstants.Roles.Admin);

            var inventory = await _db.EventSeatInventories
                .Include(i => i.Event)
                .Include(i => i.EventOccurrence)
                    .ThenInclude(o => o!.EventSeries)
                        .ThenInclude(s => s.Event)
                .FirstOrDefaultAsync(i => i.Id == request.InventoryId);

            if (inventory == null)
            {
                return NotFound(new { ok = false });
            }

            var ev = inventory.Event ?? inventory.EventOccurrence?.EventSeries.Event;
            if (ev == null || (!isAdmin && ev.OrganizerId != userId))
            {
                return Forbid();
            }

            if (inventory.Status == EventSeatInventoryStatus.Sold)
            {
                return Conflict(new { ok = false, message = "Sold seats cannot be changed manually." });
            }

            if (inventory.Status == EventSeatInventoryStatus.Reserved)
            {
                return Conflict(new { ok = false, message = "Reserved seats cannot be changed until the hold expires." });
            }

            inventory.Status = request.Status;
            inventory.ReservedUntil = null;
            inventory.ReservedByUserId = null;
            inventory.TicketId = null;
            await _db.SaveChangesAsync();

            return Ok(new { ok = true, inventory.Id, Status = inventory.Status.ToString() });
        }
    }

    public class SeatReservationRequest
    {
        public int EventId { get; set; }
        public int? EventOccurrenceId { get; set; }
        public int SeatId { get; set; }
    }

    public class SeatReleaseRequest
    {
        public int InventoryId { get; set; }
    }

    public class SeatStatusRequest
    {
        public int InventoryId { get; set; }
        public EventSeatInventoryStatus Status { get; set; }
    }
}
