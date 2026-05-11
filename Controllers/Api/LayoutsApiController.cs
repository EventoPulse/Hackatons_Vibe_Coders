using EventsApp.Common;
using EventsApp.Data;
using EventsApp.Models;
using EventsApp.Services;
using EventsApp.Services.AI;
using EventsApp.ViewModels.Layouts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventsApp.Controllers.Api
{
    [ApiController]
    [Route("api/layouts")]
    [Authorize(Roles = GlobalConstants.Roles.Admin + "," + GlobalConstants.Roles.Organizer)]
    public class LayoutsApiController : ControllerBase
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() },
        };
        private const int MaxAiDescriptionLength = 1200;
        private const long MaxAiImageBytes = 5L * 1024 * 1024;

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILayoutService _layouts;
        private readonly ILayoutAiService _layoutAi;

        public LayoutsApiController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            ILayoutService layouts,
            ILayoutAiService layoutAi)
        {
            _db = db;
            _userManager = userManager;
            _layouts = layouts;
            _layoutAi = layoutAi;
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
                layoutJson = BuildLayoutJson(layout),
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
            if (Enum.TryParse<VenueLayoutStatus>(request.Status, true, out var createStatus)) layout.Status = createStatus;
            ApplyLayoutJson(layout, request.LayoutJson);
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

            if (await _layouts.LayoutHasSoldSeatsAsync(id))
            {
                var versioned = new VenueLayout
                {
                    OrganizerId = layout.OrganizerId,
                    VenueName = request.VenueName.Trim(),
                    Name = request.Name.Trim(),
                    Version = layout.Version + 1,
                    Status = Enum.TryParse<VenueLayoutStatus>(request.Status, true, out var versionStatus) ? versionStatus : layout.Status,
                };
                ApplyLayoutJson(versioned, request.LayoutJson);
                _db.VenueLayouts.Add(versioned);
                await _db.SaveChangesAsync();
                return Ok(new { id = versioned.Id, versioned = true });
            }

            layout = await _db.VenueLayouts
                .Include(l => l.Sections)
                .Include(l => l.Seats)
                .FirstAsync(l => l.Id == id);

            layout.VenueName = request.VenueName.Trim();
            layout.Name = request.Name.Trim();
            if (Enum.TryParse<VenueLayoutStatus>(request.Status, true, out var status)) layout.Status = status;
            layout.UpdatedAt = DateTime.UtcNow;
            _db.Seats.RemoveRange(layout.Seats);
            _db.LayoutSections.RemoveRange(layout.Sections);
            ApplyLayoutJson(layout, request.LayoutJson);
            await _db.SaveChangesAsync();
            return Ok(new { id = layout.Id });
        }

        [HttpPost("ai-generate")]
        [RequestSizeLimit(MaxAiImageBytes + 64 * 1024)]
        public async Task<IActionResult> AiGenerate([FromForm] string? description, [FromForm] IFormFile? image, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(description) && description.Length > MaxAiDescriptionLength)
                return BadRequest(new { ok = false, fallback = true, message = $"Описанието е твърде дълго. Максимумът е {MaxAiDescriptionLength} символа." });
            if (image?.Length > MaxAiImageBytes)
                return BadRequest(new { ok = false, fallback = true, message = "Снимката е твърде голяма. Максимумът е 5 MB." });
            if (image != null && image.Length > 0 && (string.IsNullOrWhiteSpace(image.ContentType) || !image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)))
                return BadRequest(new { ok = false, fallback = true, message = "Може да се качва само снимка." });
            if ((string.IsNullOrWhiteSpace(description) && (image == null || image.Length == 0)) || !_layoutAi.IsEnabled)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { ok = false, fallback = true, message = string.IsNullOrWhiteSpace(_layoutAi.LastError) ? "AI не е наличен. Използвай локалния генератор." : _layoutAi.LastError });

            var layout = await _layoutAi.GenerateLayoutAsync(description, image, cancellationToken);
            if (layout == null)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { ok = false, fallback = true, message = string.IsNullOrWhiteSpace(_layoutAi.LastError) ? "AI не успя да върне валиден layout." : _layoutAi.LastError });

            return new JsonResult(new { ok = true, layout }, JsonOptions);
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

        private static void ApplyLayoutJson(VenueLayout layout, string? json)
        {
            var parsed = string.IsNullOrWhiteSpace(json)
                ? DefaultLayout()
                : JsonSerializer.Deserialize<VenueLayoutJsonModel>(json, JsonOptions) ?? DefaultLayout();

            foreach (var inputSection in parsed.Sections)
            {
                var section = new LayoutSection
                {
                    VenueLayout = layout,
                    Name = string.IsNullOrWhiteSpace(inputSection.Name) ? "Секция" : inputSection.Name.Trim(),
                    FloorName = string.IsNullOrWhiteSpace(inputSection.FloorName) ? "Floor 1" : inputSection.FloorName.Trim(),
                    Type = inputSection.Type,
                    Shape = NormalizeShape(inputSection.Shape),
                    Capacity = inputSection.Seats.Count > 0
                        ? inputSection.Seats.Sum(s => s.IsCapacityUnlimited ? 0 : Math.Max(1, s.Capacity))
                        : Math.Max(0, inputSection.Capacity),
                    PriceModifier = inputSection.PriceModifier,
                    ColorHex = NormalizeColorHex(inputSection.ColorHex, DefaultSectionColor(inputSection.Type)),
                    X = inputSection.X,
                    Y = inputSection.Y,
                    Width = inputSection.Width <= 0 ? 220 : inputSection.Width,
                    Height = inputSection.Height <= 0 ? 140 : inputSection.Height,
                    Rotation = inputSection.Rotation,
                };
                layout.Sections.Add(section);

                foreach (var inputSeat in inputSection.Seats.Take(800))
                {
                    layout.Seats.Add(new Seat
                    {
                        VenueLayout = layout,
                        Section = section,
                        Row = string.IsNullOrWhiteSpace(inputSeat.Row) ? "A" : inputSeat.Row.Trim(),
                        Number = string.IsNullOrWhiteSpace(inputSeat.Number) ? "1" : inputSeat.Number.Trim(),
                        Label = string.IsNullOrWhiteSpace(inputSeat.Label) ? null : inputSeat.Label.Trim(),
                        X = inputSeat.X,
                        Y = inputSeat.Y,
                        Radius = inputSeat.Radius <= 0 ? 16 : inputSeat.Radius,
                        Rotation = inputSeat.Rotation,
                        Capacity = Math.Max(1, inputSeat.Capacity),
                        IsCapacityUnlimited = inputSeat.IsCapacityUnlimited,
                        SeatType = inputSeat.SeatType,
                        Status = inputSeat.Status,
                    });
                }
            }
        }

        private static VenueLayoutJsonModel DefaultLayout() => new()
        {
            CanvasWidth = 1200,
            CanvasHeight = 820,
            Floors = new List<LayoutFloorJsonModel> { new() { ClientId = "floor-1", Name = "Партер" } },
            Sections = new List<LayoutSectionJsonModel>
            {
                new() { ClientId = "stage-1", Name = "СЦЕНА", FloorId = "floor-1", FloorName = "Партер", Shape = "Stage", Capacity = 0, ColorHex = "#e5e7eb", X = 330, Y = 54, Width = 520, Height = 118 },
                new() { ClientId = "section-1", Name = "Основна секция", FloorId = "floor-1", FloorName = "Партер", Type = LayoutSectionType.Seated, Shape = "Rounded", Capacity = 40, ColorHex = "#df5f83", X = 150, Y = 250, Width = 820, Height = 260 },
            },
        };

        private static string BuildLayoutJson(VenueLayout layout)
        {
            var floors = layout.Sections
                .Select(s => string.IsNullOrWhiteSpace(s.FloorName) ? "Floor 1" : s.FloorName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (floors.Count == 0) floors.Add("Floor 1");

            var payload = new VenueLayoutJsonModel
            {
                CanvasWidth = 1200,
                CanvasHeight = 820,
                Floors = floors.Select((floorName, index) => new LayoutFloorJsonModel
                {
                    ClientId = "floor-" + (index + 1),
                    Name = floorName,
                }).ToList(),
                Sections = layout.Sections.OrderBy(s => s.Id).Select(section =>
                {
                    var floorName = string.IsNullOrWhiteSpace(section.FloorName) ? "Floor 1" : section.FloorName;
                    var floorIndex = Math.Max(0, floors.FindIndex(f => string.Equals(f, floorName, StringComparison.OrdinalIgnoreCase)));
                    return new LayoutSectionJsonModel
                    {
                        Id = section.Id,
                        ClientId = "section-" + section.Id,
                        Name = section.Name,
                        FloorId = "floor-" + (floorIndex + 1),
                        FloorName = floorName,
                        Type = section.Type,
                        Shape = NormalizeShape(section.Shape),
                        Capacity = section.Capacity,
                        PriceModifier = section.PriceModifier,
                        ColorHex = NormalizeColorHex(section.ColorHex, DefaultSectionColor(section.Type)),
                        X = section.X,
                        Y = section.Y,
                        Width = section.Width,
                        Height = section.Height,
                        Rotation = section.Rotation,
                        Seats = layout.Seats.Where(seat => seat.SectionId == section.Id).OrderBy(seat => seat.Row).ThenBy(seat => seat.Number).Select(seat => new SeatJsonModel
                        {
                            Id = seat.Id,
                            Row = seat.Row,
                            Number = seat.Number,
                            Label = seat.Label,
                            X = seat.X,
                            Y = seat.Y,
                            Radius = seat.Radius <= 0 ? 16 : seat.Radius,
                            Rotation = seat.Rotation,
                            Capacity = Math.Max(1, seat.Capacity),
                            IsCapacityUnlimited = seat.IsCapacityUnlimited,
                            SeatType = seat.SeatType,
                            Status = seat.Status,
                        }).ToList(),
                    };
                }).ToList(),
            };

            return JsonSerializer.Serialize(payload, JsonOptions);
        }

        private static string NormalizeShape(string? value)
        {
            var shape = string.IsNullOrWhiteSpace(value) ? "Rectangle" : value.Trim();
            return shape.Length > 32 ? shape[..32] : shape;
        }

        private static string NormalizeColorHex(string? value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            var color = value.Trim();
            if (!color.StartsWith("#", StringComparison.Ordinal)) color = "#" + color;
            return System.Text.RegularExpressions.Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$") ? color.ToLowerInvariant() : fallback;
        }

        private static string DefaultSectionColor(LayoutSectionType type) => type switch
        {
            LayoutSectionType.VIP => "#f59e0b",
            LayoutSectionType.Table => "#0d9488",
            LayoutSectionType.Standing => "#22c55e",
            _ => "#2456ff",
        };
    }
}

public record LayoutSaveRequest(string VenueName, string Name, string? Status, string? LayoutJson);
