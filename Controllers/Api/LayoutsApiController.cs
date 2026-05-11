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
    [Route("api/layouts")]
    [Authorize(Roles = GlobalConstants.Roles.Admin + "," + GlobalConstants.Roles.Organizer)]
    public class LayoutsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILayoutService _layouts;

        public LayoutsApiController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            ILayoutService layouts)
        {
            _db = db;
            _userManager = userManager;
            _layouts = layouts;
        }

        [HttpGet]
        public async Task<IActionResult> Mine()
        {
            var userId = _userManager.GetUserId(User)!;
            var isAdmin = User.IsInRole(GlobalConstants.Roles.Admin);

            var query = _db.VenueLayouts.AsNoTracking();
            if (!isAdmin)
            {
                query = query.Where(l => l.OrganizerId == userId);
            }

            var layouts = await query
                .OrderBy(l => l.VenueName)
                .ThenBy(l => l.Name)
                .Select(l => new
                {
                    l.Id,
                    l.VenueName,
                    l.Name,
                    l.Version,
                    Status = l.Status.ToString(),
                    Seats = l.Seats.Count,
                })
                .ToListAsync();

            return Ok(layouts);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var isAdmin = User.IsInRole(GlobalConstants.Roles.Admin);
            var layout = await _db.VenueLayouts
                .AsNoTracking()
                .Include(l => l.Sections)
                .Include(l => l.Seats)
                .FirstOrDefaultAsync(l => l.Id == id);
            if (layout == null) return NotFound();
            if (!isAdmin && layout.OrganizerId != userId) return Forbid();

            return Ok(new
            {
                layout.Id,
                layout.VenueName,
                layout.Name,
                layout.Version,
                Status = layout.Status.ToString(),
                sections = layout.Sections.OrderBy(s => s.Id).Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.FloorName,
                    type = s.Type.ToString(),
                    s.Shape,
                    s.Capacity,
                    s.PriceModifier,
                    s.ColorHex,
                    s.X,
                    s.Y,
                    s.Width,
                    s.Height,
                    s.Rotation,
                }),
                seats = layout.Seats.OrderBy(s => s.Id).Select(s => new
                {
                    s.Id,
                    s.SectionId,
                    s.Row,
                    s.Number,
                    s.Label,
                    s.X,
                    s.Y,
                    s.Radius,
                    s.Rotation,
                    s.Capacity,
                    s.IsCapacityUnlimited,
                    seatType = s.SeatType.ToString(),
                    status = s.Status.ToString(),
                }),
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LayoutSaveRequest request)
        {
            var userId = _userManager.GetUserId(User)!;
            if (string.IsNullOrWhiteSpace(request.VenueName) || string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { error = "Попълни име на зала и схема." });

            var layout = new VenueLayout
            {
                OrganizerId = userId,
                VenueName = request.VenueName.Trim(),
                Name = request.Name.Trim(),
                Status = VenueLayoutStatus.Draft,
            };
            _db.VenueLayouts.Add(layout);
            await _db.SaveChangesAsync();
            return Ok(new { id = layout.Id });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] LayoutSaveRequest request)
        {
            var userId = _userManager.GetUserId(User)!;
            var isAdmin = User.IsInRole(GlobalConstants.Roles.Admin);
            var layout = await _db.VenueLayouts.FirstOrDefaultAsync(l => l.Id == id);
            if (layout == null) return NotFound();
            if (!isAdmin && layout.OrganizerId != userId) return Forbid();
            if (await _layouts.LayoutHasSoldSeatsAsync(id)) return Conflict(new { error = "Схемата има продадени места и не може да се редактира." });

            layout.VenueName = request.VenueName.Trim();
            layout.Name = request.Name.Trim();
            if (Enum.TryParse<VenueLayoutStatus>(request.Status, true, out var status)) layout.Status = status;
            layout.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { id = layout.Id });
        }

        [HttpPost("{layoutId:int}/duplicate")]
        public async Task<IActionResult> Duplicate(int layoutId)
        {
            var copy = await _layouts.DuplicateLayoutAsync(
                layoutId,
                _userManager.GetUserId(User)!,
                User.IsInRole(GlobalConstants.Roles.Admin));

            return copy == null
                ? NotFound(new { ok = false })
                : Ok(new { ok = true, id = copy.Id });
        }

        [HttpPost("{layoutId:int}/assign/{eventId:int}")]
        public async Task<IActionResult> Assign(int layoutId, int eventId)
        {
            var userId = _userManager.GetUserId(User)!;
            var isAdmin = User.IsInRole(GlobalConstants.Roles.Admin);

            var layout = await _db.VenueLayouts.FirstOrDefaultAsync(l => l.Id == layoutId);
            var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (layout == null || ev == null) return NotFound(new { ok = false });
            if (!isAdmin && (layout.OrganizerId != userId || ev.OrganizerId != userId)) return Forbid();

            ev.VenueLayoutId = layout.Id;
            ev.TicketingMode = EventTicketingMode.SeatedLayout;
            await _db.SaveChangesAsync();
            return Ok(new { ok = true });
        }
    }
}

public record LayoutSaveRequest(string VenueName, string Name, string? Status);
