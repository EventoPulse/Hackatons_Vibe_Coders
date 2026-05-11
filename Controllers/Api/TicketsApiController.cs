using EventsApp.Common;
using EventsApp.Data;
using EventsApp.Models;
using EventsApp.Services;
using EventsApp.ViewModels.Tickets;
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
        private readonly ITicketDocumentService _docs;

        public TicketsApiController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ITicketDocumentService docs)
        {
            _db = db;
            _userManager = userManager;
            _docs = docs;
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
                pdfUrl = $"/api/tickets/{ut.Id}/pdf",
                isUsed = ut.IsUsed,
                purchasedAt = ut.CreatedAt,
            }));
        }

        [HttpGet("event/{eventId:int}")]
        public async Task<IActionResult> ForEvent(int eventId)
        {
            var userId = _userManager.GetUserId(User)!;
            var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId);
            if (ev == null) return NotFound();
            if (!CanManageEvent(userId, ev)) return Forbid();

            var tickets = await _db.Tickets
                .AsNoTracking()
                .Where(t => t.EventId == eventId)
                .OrderBy(t => t.CreatedAt)
                .Select(t => new
                {
                    id = t.Id,
                    eventId = t.EventId,
                    name = t.Name,
                    description = t.Description,
                    price = t.Price,
                    quantityTotal = t.QuantityTotal,
                    quantityRemaining = t.QuantityRemaining,
                    soldCount = t.QuantityTotal - t.QuantityRemaining,
                    imageUrl = t.ImageUrl,
                    isActive = t.IsActive,
                    requiresAttendeeNames = t.RequiresAttendeeNames,
                    createdAt = t.CreatedAt,
                })
                .ToListAsync();

            return Ok(new { eventId = ev.Id, eventTitle = ev.Title, tickets });
        }

        [HttpPost("event/{eventId:int}")]
        public async Task<IActionResult> CreateForEvent(int eventId, [FromBody] TicketRequest request)
        {
            var userId = _userManager.GetUserId(User)!;
            var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (ev == null) return NotFound();
            if (!CanManageEvent(userId, ev)) return Forbid();

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { error = "Името на билета е задължително." });
            if (request.Price < 0 || request.QuantityTotal < 0)
                return BadRequest(new { error = "Цената и количеството не могат да бъдат отрицателни." });

            var ticket = new Ticket
            {
                EventId = eventId,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                Price = request.Price,
                QuantityTotal = request.QuantityTotal,
                QuantityRemaining = request.QuantityTotal,
                ImageUrl = request.ImageUrl?.Trim(),
                IsActive = request.IsActive,
                RequiresAttendeeNames = request.RequiresAttendeeNames,
            };

            _db.Tickets.Add(ticket);
            await _db.SaveChangesAsync();

            return Ok(new { id = ticket.Id, eventId = ticket.EventId, name = ticket.Name });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateTicket(Guid id, [FromBody] TicketRequest request)
        {
            var userId = _userManager.GetUserId(User)!;
            var ticket = await _db.Tickets.Include(t => t.Event).FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null) return NotFound();
            if (!CanManageEvent(userId, ticket.Event)) return Forbid();

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { error = "Името на билета е задължително." });
            if (request.Price < 0 || request.QuantityTotal < 0)
                return BadRequest(new { error = "Цената и количеството не могат да бъдат отрицателни." });

            var sold = await _db.UserTickets.CountAsync(ut => ut.TicketId == id);
            if (request.QuantityTotal < sold)
                return BadRequest(new { error = "Общото количество не може да е по-малко от вече продадените билети." });

            ticket.Name = request.Name.Trim();
            ticket.Description = request.Description?.Trim();
            ticket.Price = request.Price;
            ticket.QuantityTotal = request.QuantityTotal;
            ticket.QuantityRemaining = Math.Max(0, request.QuantityTotal - sold);
            ticket.ImageUrl = request.ImageUrl?.Trim();
            ticket.IsActive = request.IsActive;
            ticket.RequiresAttendeeNames = request.RequiresAttendeeNames;

            await _db.SaveChangesAsync();
            return Ok(new { id = ticket.Id, eventId = ticket.EventId, name = ticket.Name });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteTicket(Guid id)
        {
            var userId = _userManager.GetUserId(User)!;
            var ticket = await _db.Tickets.Include(t => t.Event).FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null) return NotFound();
            if (!CanManageEvent(userId, ticket.Event)) return Forbid();

            if (await _db.UserTickets.AnyAsync(ut => ut.TicketId == id))
                return Conflict(new { error = "Билетът вече има покупки и не може да бъде изтрит." });

            _db.Tickets.Remove(ticket);
            await _db.SaveChangesAsync();
            return Ok(new { deleted = true });
        }

        [HttpPost("{id:guid}/buy")]
        public async Task<IActionResult> Buy(Guid id, [FromBody] BuyTicketRequest request)
        {
            var userId = _userManager.GetUserId(User)!;
            var quantity = Math.Clamp(request.Quantity, 1, 10);
            var ticket = await _db.Tickets
                .Include(t => t.Event)
                .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);

            if (ticket == null) return NotFound();
            if (ticket.QuantityRemaining < quantity)
                return BadRequest(new { error = "Няма достатъчно налични билети." });

            await using var tx = await _db.Database.BeginTransactionAsync();

            ticket.QuantityRemaining -= quantity;
            var transaction = new Transaction
            {
                UserId = userId,
                TotalAmount = ticket.Price * quantity,
                Status = GlobalConstants.TransactionStatuses.Paid,
            };
            _db.Transactions.Add(transaction);

            var groupId = Guid.NewGuid();
            for (var i = 0; i < quantity; i++)
            {
                _db.UserTickets.Add(new UserTicket
                {
                    TicketId = ticket.Id,
                    TransactionId = transaction.Id,
                    PricePaid = ticket.Price,
                    QrCode = $"EVT-{Guid.NewGuid():N}",
                    PurchaseGroupId = groupId,
                    IsPrimaryInPurchase = i == 0,
                });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { transactionId = transaction.Id, ticketsCount = quantity });
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
                pdfUrl = $"/api/tickets/{ut.Id}/pdf",
                isUsed = ut.IsUsed,
                purchasedAt = ut.CreatedAt,
            });
        }

        // GET /api/tickets/{id}/qr
        [HttpGet("{id:guid}/qr")]
        public async Task<IActionResult> Qr(Guid id)
        {
            var userId = _userManager.GetUserId(User)!;
            var ut = await _db.UserTickets
                .AsNoTracking()
                .Include(x => x.Ticket).ThenInclude(t => t.Event)
                .Include(x => x.Transaction)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (ut == null) return NotFound();
            if (!await CanAccessTicketAsync(userId, ut)) return Forbid();
            if (string.IsNullOrWhiteSpace(ut.QrCode)) return NotFound();

            return File(_docs.GenerateQrPng(ut.QrCode), "image/png");
        }

        // GET /api/tickets/{id}/pdf
        [HttpGet("{id:guid}/pdf")]
        public async Task<IActionResult> Pdf(Guid id)
        {
            var userId = _userManager.GetUserId(User)!;
            var ut = await TicketDetailsQuery().FirstOrDefaultAsync(x => x.Id == id);

            if (ut == null) return NotFound();
            if (!await CanAccessTicketAsync(userId, ut)) return Forbid();

            var vm = ToDetails(ut);
            return File(_docs.GenerateTicketPdf(vm), "application/pdf", $"Evento-Ticket-{ut.Id}.pdf");
        }

        // POST /api/tickets/validate
        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] ValidateTicketDto dto)
        {
            var userId = _userManager.GetUserId(User)!;
            var isAdmin = User.IsInRole("Admin");
            var code = (dto.QrCode ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(new { valid = false, message = "Въведи или сканирай QR кода на билета." });

            var ut = await TicketDetailsQuery()
                .Include(x => x.UsedByOrganizer)
                .FirstOrDefaultAsync(x => x.QrCode == code);

            if (ut == null)
                return NotFound(new { valid = false, notFound = true, message = "Не е намерен билет с този QR код." });

            if (!isAdmin && !await CanValidateEventAsync(userId, ut.Ticket.Event))
                return Forbid();

            var details = ToDetails(ut);
            if (ut.IsUsed)
            {
                return Ok(new
                {
                    valid = false,
                    alreadyUsed = true,
                    message = $"Билетът вече е използван на {ut.UsedAt:dd.MM.yyyy HH:mm}.",
                    ticket = details,
                });
            }

            if (ut.EventOccurrence?.Status == EventOccurrenceStatus.Cancelled)
            {
                return Ok(new { valid = false, message = "Билетът е за отменена дата.", ticket = details });
            }

            if (!dto.Confirm)
            {
                return Ok(new
                {
                    valid = false,
                    requiresConfirmation = true,
                    message = "Билетът е намерен. Провери данните и потвърди валидирането.",
                    ticket = details,
                });
            }

            ut.IsUsed = true;
            ut.UsedAt = DateTime.UtcNow;
            ut.UsedByOrganizerId = userId;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                valid = true,
                message = "Билетът е валидиран успешно.",
                ticket = ToDetails(ut),
            });
        }

        private IQueryable<UserTicket> TicketDetailsQuery()
            => _db.UserTickets
                .Include(x => x.Ticket).ThenInclude(t => t.Event)
                .Include(x => x.Ticket).ThenInclude(t => t.SectionPrices)
                .Include(x => x.EventOccurrence)
                .Include(x => x.Seat).ThenInclude(s => s!.Section)
                .Include(x => x.Transaction).ThenInclude(t => t.User);

        private async Task<bool> CanAccessTicketAsync(string userId, UserTicket ut)
        {
            if (User.IsInRole("Admin")) return true;
            if (ut.Transaction.UserId == userId) return true;
            if (ut.Ticket.Event.OrganizerId == userId) return true;
            return await CanValidateEventAsync(userId, ut.Ticket.Event);
        }

        private async Task<bool> CanValidateEventAsync(string userId, Event ev)
        {
            if (ev.OrganizerId == userId) return true;
            if (!ev.OrganizerProfileId.HasValue) return false;

            return await _db.OrganizerValidatorAssignments
                .AsNoTracking()
                .AnyAsync(a =>
                    a.OrganizerId == ev.OrganizerId &&
                    a.ValidatorUserId == userId &&
                    a.IsActive &&
                    a.OrganizerProfileId == ev.OrganizerProfileId.Value);
        }

        private static UserTicketDetailsViewModel ToDetails(UserTicket ut)
        {
            return new UserTicketDetailsViewModel
            {
                Id = ut.Id,
                TicketId = ut.TicketId,
                TransactionId = ut.TransactionId,
                TicketName = ut.Ticket.Name,
                EventTitle = ut.Ticket.Event.Title,
                EventId = ut.Ticket.EventId,
                EventOccurrenceId = ut.EventOccurrenceId,
                Address = ut.Ticket.Event.Address,
                City = ut.Ticket.Event.City,
                StartTime = ut.EventOccurrence?.StartDateTime ?? ut.Ticket.Event.StartTime,
                EndTime = ut.EventOccurrence?.EndDateTime ?? ut.Ticket.Event.EndTime,
                SeatLabel = ut.Seat != null ? GetSeatLabel(ut.Seat) : null,
                AttendeeName = ut.AttendeeName,
                PurchaseGroupId = ut.PurchaseGroupId,
                IsPrimaryInPurchase = ut.IsPrimaryInPurchase,
                Price = ut.PricePaid > 0 ? ut.PricePaid : ut.Ticket.Price,
                TransactionStatus = ut.Transaction.Status,
                QrCode = ut.QrCode,
                IsUsed = ut.IsUsed,
                CreatedAt = ut.CreatedAt,
                UsedAt = ut.UsedAt,
                UsedByOrganizerName = ut.UsedByOrganizer?.UserName,
                OwnerUserName = ut.Transaction.User.UserName ?? string.Empty,
                OwnerEmail = ut.Transaction.User.Email ?? string.Empty,
            };
        }

        private static string GetSeatLabel(Seat seat)
            => string.IsNullOrWhiteSpace(seat.Label) ? seat.Row + seat.Number : seat.Label;

        private bool CanManageEvent(string userId, Event ev)
            => User.IsInRole(GlobalConstants.Roles.Admin) || ev.OrganizerId == userId;
    }
}

public record ValidateTicketDto(string? QrCode, bool Confirm);
public record TicketRequest(
    string Name,
    string? Description,
    decimal Price,
    int QuantityTotal,
    string? ImageUrl,
    bool IsActive,
    bool RequiresAttendeeNames);
public record BuyTicketRequest(int Quantity);
