using EventsApp.Common;
using EventsApp.Data;
using EventsApp.Hubs;
using EventsApp.Models;
using EventsApp.Services;
using EventsApp.Services.AI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EventsApp.Controllers.Api
{
    [ApiController]
    [Route("api/events")]
    [IgnoreAntiforgeryToken]
    public class EventsFullApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMediaUploadService _media;
        private readonly IEventDeletionService _eventDeletion;
        private readonly IRecurringEventService _recurringEvents;
        private readonly IAiSearchService _ai;
        private readonly ILayoutService _layouts;
        private readonly IHubContext<FeedHub> _feed;

        public EventsFullApiController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IMediaUploadService media,
            IEventDeletionService eventDeletion,
            IRecurringEventService recurringEvents,
            IAiSearchService ai,
            ILayoutService layouts,
            IHubContext<FeedHub> feed)
        {
            _db = db;
            _userManager = userManager;
            _media = media;
            _eventDeletion = eventDeletion;
            _recurringEvents = recurringEvents;
            _ai = ai;
            _layouts = layouts;
            _feed = feed;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
        private bool IsAdmin => User.IsInRole(GlobalConstants.Roles.Admin);

        // ── GET /api/events ──────────────────────────────────────────────────────
        [HttpGet]
        [EnableRateLimiting("public-read")]
        public async Task<IActionResult> List(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] string? genre = null,
            [FromQuery] string? city = null,
            [FromQuery] string? keyword = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] bool freeOnly = false)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);
            var userId = CurrentUserId;

            var q = _db.Events
                .AsNoTracking()
                .Where(e => e.IsApproved && e.OrganizerProfileId != null)
                .Include(e => e.Organizer)
                .Include(e => e.OrganizerProfile)
                .Include(e => e.Likes)
                .Include(e => e.Saves)
                .Include(e => e.Attendances)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var pattern = "%" + EscapeLikePattern(keyword) + "%";
                q = q.Where(e =>
                    EF.Functions.ILike(e.Title, pattern) ||
                    (e.Description != null && EF.Functions.ILike(e.Description, pattern)) ||
                    EF.Functions.ILike(e.City, pattern) ||
                    EF.Functions.ILike(e.Address, pattern) ||
                    (e.OrganizerProfile != null && EF.Functions.ILike(e.OrganizerProfile.DisplayName, pattern)) ||
                    e.Tickets.Any(t => EF.Functions.ILike(t.Name, pattern) || (t.Description != null && EF.Functions.ILike(t.Description, pattern))));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                var cityPattern = "%" + EscapeLikePattern(city) + "%";
                q = q.Where(e => EF.Functions.ILike(e.City, cityPattern));
            }

            if (!string.IsNullOrWhiteSpace(genre) && Enum.TryParse<EventGenre>(genre, true, out var genreEnum))
                q = q.Where(e => e.Genre == genreEnum);

            if (dateFrom.HasValue)
                q = q.Where(e => e.StartTime >= dateFrom.Value);

            if (dateTo.HasValue)
            {
                var inclusiveDateEnd = dateTo.Value.TimeOfDay == TimeSpan.Zero
                    ? dateTo.Value.Date.AddDays(1)
                    : dateTo.Value;

                q = dateTo.Value.TimeOfDay == TimeSpan.Zero
                    ? q.Where(e => e.StartTime < inclusiveDateEnd)
                    : q.Where(e => e.StartTime <= inclusiveDateEnd);
            }

            if (freeOnly)
                q = q.Where(e => !e.Tickets.Any(t => t.IsActive && t.Price > 0m));

            var total = await q.CountAsync();
            var events = await q
                .OrderByDescending(e => e.StartTime >= DateTime.UtcNow)
                .ThenBy(e => e.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = events.Select(e => MapToCard(e, userId)).ToList();

            return Ok(new
            {
                items,
                totalCount = total,
                page,
                pageSize,
                hasMore = (page * pageSize) < total,
            });
        }

        // ── GET /api/events/recommended ─────────────────────────────────────────
        [HttpGet("recommended")]
        [EnableRateLimiting("public-read")]
        public async Task<IActionResult> Recommended([FromQuery] int pageSize = 12)
        {
            var userId = CurrentUserId;
            var now = DateTime.UtcNow;
            pageSize = Math.Clamp(pageSize, 1, 24);

            List<object> results;

            if (userId != null)
            {
                // Collect user's genre preferences from interactions
                var likedGenres = await _db.EventLikes
                    .Where(l => l.UserId == userId)
                    .Join(_db.Events, l => l.EventId, e => e.Id, (l, e) => e.Genre)
                    .ToListAsync();

                var savedGenres = await _db.EventSaves
                    .Where(s => s.UserId == userId)
                    .Join(_db.Events, s => s.EventId, e => e.Id, (s, e) => e.Genre)
                    .ToListAsync();

                var attendedGenres = await _db.EventAttendances
                    .Where(a => a.UserId == userId)
                    .Join(_db.Events, a => a.EventId, e => e.Id, (a, e) => e.Genre)
                    .ToListAsync();

                // Score genres: attendance=3, save=2, like=1
                var genreScores = new Dictionary<EventGenre, int>();
                foreach (var g in attendedGenres) genreScores[g] = genreScores.GetValueOrDefault(g) + 3;
                foreach (var g in savedGenres)   genreScores[g] = genreScores.GetValueOrDefault(g) + 2;
                foreach (var g in likedGenres)   genreScores[g] = genreScores.GetValueOrDefault(g) + 1;

                // Collect already-seen event IDs
                var seenEventIds = new HashSet<int>(
                    likedGenres.Select((_, __) => 0) // placeholder — fetched below
                );
                var likedEventIds   = await _db.EventLikes.Where(l => l.UserId == userId).Select(l => l.EventId).ToListAsync();
                var savedEventIds   = await _db.EventSaves.Where(s => s.UserId == userId).Select(s => s.EventId).ToListAsync();
                var attendedEventIds = await _db.EventAttendances.Where(a => a.UserId == userId).Select(a => a.EventId).ToListAsync();
                seenEventIds = new HashSet<int>(likedEventIds.Concat(savedEventIds).Concat(attendedEventIds));

                if (genreScores.Count > 0)
                {
                    var preferredGenres = genreScores.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).Take(3).ToList();

                    var recommended = await _db.Events
                        .AsNoTracking()
                        .Where(e => e.IsApproved && e.StartTime > now && preferredGenres.Contains(e.Genre) && !seenEventIds.Contains(e.Id))
                        .Include(e => e.Likes)
                        .Include(e => e.Saves)
                        .Include(e => e.Attendances)
                        .Include(e => e.OrganizerProfile)
                        .Include(e => e.Organizer)
                        .OrderByDescending(e => e.Likes.Count * 1 + e.Attendances.Count(a => a.Status == EventAttendanceStatus.Going) * 3)
                        .ThenBy(e => e.StartTime)
                        .Take(pageSize)
                        .ToListAsync();

                    if (recommended.Count >= 4)
                    {
                        results = recommended.Select(e => MapToCard(e, userId)).ToList();
                        return Ok(new { items = results, isPersonalized = true });
                    }

                    // Not enough — fill with popular unseen events
                    var existingIds = recommended.Select(e => e.Id).ToHashSet();
                    var filler = await _db.Events
                        .AsNoTracking()
                        .Where(e => e.IsApproved && e.StartTime > now && !seenEventIds.Contains(e.Id) && !existingIds.Contains(e.Id))
                        .Include(e => e.Likes)
                        .Include(e => e.Saves)
                        .Include(e => e.Attendances)
                        .Include(e => e.OrganizerProfile)
                        .Include(e => e.Organizer)
                        .OrderByDescending(e => e.Likes.Count + e.Attendances.Count(a => a.Status == EventAttendanceStatus.Going) * 2)
                        .Take(pageSize - recommended.Count)
                        .ToListAsync();

                    results = recommended.Concat(filler).Select(e => MapToCard(e, userId)).ToList();
                    return Ok(new { items = results, isPersonalized = results.Count > 0 });
                }
            }

            // Fallback: popular upcoming events
            var popular = await _db.Events
                .AsNoTracking()
                .Where(e => e.IsApproved && e.StartTime > now)
                .Include(e => e.Likes)
                .Include(e => e.Saves)
                .Include(e => e.Attendances)
                .Include(e => e.OrganizerProfile)
                .Include(e => e.Organizer)
                .OrderByDescending(e => e.Likes.Count + e.Attendances.Count(a => a.Status == EventAttendanceStatus.Going) * 2)
                .ThenBy(e => e.StartTime)
                .Take(pageSize)
                .ToListAsync();

            results = popular.Select(e => MapToCard(e, userId)).ToList();
            return Ok(new { items = results, isPersonalized = false });
        }

        // ── GET /api/events/{id} ─────────────────────────────────────────────────
        [HttpGet("{id:int}")]
        [EnableRateLimiting("public-read")]
        public async Task<IActionResult> Details(int id)
        {
            var userId = CurrentUserId;

            var ev = await _db.Events
                .AsNoTracking()
                .Where(e => e.Id == id)
                .Include(e => e.Organizer)
                .Include(e => e.OrganizerProfile)
                .Include(e => e.Images)
                .Include(e => e.Likes)
                .Include(e => e.Saves)
                .Include(e => e.Attendances)
                .Include(e => e.Tickets).ThenInclude(t => t.SectionPrices)
                .Include(e => e.EventSeries).ThenInclude(s => s!.Occurrences)
                .FirstOrDefaultAsync();

            if (ev == null) return NotFound(new { error = "Събитието не е намерено." });
            if (!ev.IsApproved && !IsAdmin && ev.OrganizerId != userId)
                return NotFound(new { error = "Събитието не е намерено." });

            var comments = await _db.EventComments
                .AsNoTracking()
                .Where(c => c.EventId == id && c.ParentCommentId == null)
                .Include(c => c.User)
                .Include(c => c.Likes)
                .Include(c => c.Replies).ThenInclude(r => r.User)
                .OrderByDescending(c => c.Likes.Count)
                .ThenByDescending(c => c.CreatedAt)
                .Take(20)
                .ToListAsync();

            var similar = await _db.Events
                .AsNoTracking()
                .Where(e => e.Id != id && e.IsApproved && e.Genre == ev.Genre && e.StartTime > DateTime.UtcNow)
                .Include(e => e.Organizer)
                .Include(e => e.Likes)
                .Include(e => e.Saves)
                .Include(e => e.Attendances)
                .OrderBy(e => e.StartTime)
                .Take(6)
                .ToListAsync();

            var canEdit = IsAdmin || ev.OrganizerId == userId;
            var hasPendingChanges = await _db.EventChangeRequests
                .AsNoTracking()
                .AnyAsync(r => r.EventId == id && r.Status == EventChangeRequestStatus.Pending);

            return Ok(new
            {
                id = ev.Id,
                title = ev.Title,
                description = ev.Description,
                startTime = ev.StartTime,
                endTime = ev.EndTime,
                genre = ev.Genre.ToString(),
                imageUrl = ev.ImageUrl,
                address = ev.Address,
                city = ev.City,
                latitude = ev.Latitude,
                longitude = ev.Longitude,
                organizerId = ev.OrganizerId,
                organizerProfileId = ev.OrganizerProfileId,
                businessWorkspaceId = ev.BusinessWorkspaceId,
                organizerName = OrganizerDisplayName(ev),
                imageUrls = ev.Images.Select(i => i.ImageUrl).ToArray(),
                likesCount = ev.Likes.Count,
                savesCount = ev.Saves.Count,
                goingCount = ev.Attendances.Count(a => a.Status == EventAttendanceStatus.Going),
                interestedCount = ev.Attendances.Count(a => a.Status == EventAttendanceStatus.Interested),
                commentsCount = await _db.EventComments.CountAsync(c => c.EventId == id),
                isLiked = userId != null && ev.Likes.Any(l => l.UserId == userId),
                isSaved = userId != null && ev.Saves.Any(s => s.UserId == userId),
                userAttendanceStatus = userId != null
                    ? ev.Attendances.FirstOrDefault(a => a.UserId == userId)?.Status.ToString()
                    : null,
                canEdit,
                canDelete = canEdit,
                canManageTickets = canEdit,
                isRecurring = ev.EventSeries != null,
                ticketingMode = ev.TicketingMode.ToString(),
                venueLayoutId = ev.VenueLayoutId,
                tickets = ev.Tickets.Select(t => new
                {
                    id = t.Id,
                    name = t.Name,
                    price = t.Price,
                    currency = "BGN",
                    description = t.Description,
                }).ToArray(),
                occurrences = ev.EventSeries?.Occurrences
                    .OrderBy(o => o.StartDateTime)
                    .Select(o => new
                    {
                        id = o.Id,
                        startDateTime = o.StartDateTime,
                        endDateTime = o.EndDateTime,
                        status = o.Status.ToString(),
                        isAvailable = o.Status == EventOccurrenceStatus.Scheduled && o.StartDateTime > DateTime.UtcNow,
                    })
                    .ToArray() ?? Array.Empty<object>(),
                comments = comments.Select(c => MapComment(c, userId)).ToArray(),
                similarEvents = similar.Select(e => MapToCard(e, userId)).ToArray(),
                isApproved = ev.IsApproved,
                hasPendingChanges,
            });
        }

        // ── POST /api/events/{id}/like ───────────────────────────────────────────
        [HttpPost("{id:int}/like")]
        [Authorize(Policy = "ApiAuth")]
        [EnableRateLimiting("interactions")]
        public async Task<IActionResult> Like(int id)
        {
            var userId = CurrentUserId!;
            var ev = await _db.Events.FindAsync(id);
            if (ev == null) return NotFound();

            var exists = await _db.EventLikes.AnyAsync(l => l.EventId == id && l.UserId == userId);
            if (!exists)
            {
                _db.EventLikes.Add(new EventLike { EventId = id, UserId = userId, CreatedAt = DateTime.UtcNow });
                await _db.SaveChangesAsync();
            }

            var count = await _db.EventLikes.CountAsync(l => l.EventId == id);
            await _feed.Clients.Group($"event:{id}").SendAsync("EventLiked", new { eventId = id, likesCount = count });
            return Ok(new { likesCount = count, isLiked = true });
        }

        // ── POST /api/events/{id}/unlike ─────────────────────────────────────────
        [HttpPost("{id:int}/unlike")]
        [Authorize(Policy = "ApiAuth")]
        [EnableRateLimiting("interactions")]
        public async Task<IActionResult> Unlike(int id)
        {
            var userId = CurrentUserId!;
            var like = await _db.EventLikes.FirstOrDefaultAsync(l => l.EventId == id && l.UserId == userId);
            if (like != null)
            {
                _db.EventLikes.Remove(like);
                await _db.SaveChangesAsync();
            }
            var count = await _db.EventLikes.CountAsync(l => l.EventId == id);
            await _feed.Clients.Group($"event:{id}").SendAsync("EventLiked", new { eventId = id, likesCount = count });
            return Ok(new { likesCount = count, isLiked = false });
        }

        // ── POST /api/events/{id}/save ───────────────────────────────────────────
        [HttpPost("{id:int}/save")]
        [Authorize(Policy = "ApiAuth")]
        [EnableRateLimiting("interactions")]
        public async Task<IActionResult> Save(int id)
        {
            var userId = CurrentUserId!;
            if (!await _db.Events.AnyAsync(e => e.Id == id)) return NotFound();

            var exists = await _db.EventSaves.AnyAsync(s => s.EventId == id && s.UserId == userId);
            if (!exists)
            {
                _db.EventSaves.Add(new EventSave { EventId = id, UserId = userId, CreatedAt = DateTime.UtcNow });
                await _db.SaveChangesAsync();
            }
            var count = await _db.EventSaves.CountAsync(s => s.EventId == id);
            return Ok(new { savesCount = count, isSaved = true });
        }

        // ── POST /api/events/{id}/unsave ─────────────────────────────────────────
        [HttpPost("{id:int}/unsave")]
        [Authorize(Policy = "ApiAuth")]
        [EnableRateLimiting("interactions")]
        public async Task<IActionResult> Unsave(int id)
        {
            var userId = CurrentUserId!;
            var save = await _db.EventSaves.FirstOrDefaultAsync(s => s.EventId == id && s.UserId == userId);
            if (save != null) { _db.EventSaves.Remove(save); await _db.SaveChangesAsync(); }
            var count = await _db.EventSaves.CountAsync(s => s.EventId == id);
            return Ok(new { savesCount = count, isSaved = false });
        }

        // ── POST /api/events/{id}/attend ─────────────────────────────────────────
        [HttpPost("{id:int}/attend")]
        [Authorize(Policy = "ApiAuth")]
        [EnableRateLimiting("interactions")]
        public async Task<IActionResult> Attend(int id, [FromBody] AttendRequest request)
        {
            var userId = CurrentUserId!;
            if (!await _db.Events.AnyAsync(e => e.Id == id)) return NotFound();

            if (!Enum.TryParse<EventAttendanceStatus>(request.Status, true, out var status))
                return BadRequest(new { error = "Невалиден статус. Използвай Going или Interested." });

            var att = await _db.EventAttendances.FirstOrDefaultAsync(a => a.EventId == id && a.UserId == userId);
            if (att == null)
            {
                _db.EventAttendances.Add(new EventAttendance { EventId = id, UserId = userId, Status = status, CreatedAt = DateTime.UtcNow });
            }
            else
            {
                att.Status = status;
            }
            await _db.SaveChangesAsync();

            return Ok(new
            {
                userAttendanceStatus = status.ToString(),
                goingCount = await _db.EventAttendances.CountAsync(a => a.EventId == id && a.Status == EventAttendanceStatus.Going),
                interestedCount = await _db.EventAttendances.CountAsync(a => a.EventId == id && a.Status == EventAttendanceStatus.Interested),
            });
        }

        // ── DELETE /api/events/{id}/attend ───────────────────────────────────────
        [HttpDelete("{id:int}/attend")]
        [Authorize(Policy = "ApiAuth")]
        [EnableRateLimiting("interactions")]
        public async Task<IActionResult> Unattend(int id)
        {
            var userId = CurrentUserId!;
            var att = await _db.EventAttendances.FirstOrDefaultAsync(a => a.EventId == id && a.UserId == userId);
            if (att != null) { _db.EventAttendances.Remove(att); await _db.SaveChangesAsync(); }
            return Ok(new
            {
                userAttendanceStatus = (string?)null,
                goingCount = await _db.EventAttendances.CountAsync(a => a.EventId == id && a.Status == EventAttendanceStatus.Going),
                interestedCount = await _db.EventAttendances.CountAsync(a => a.EventId == id && a.Status == EventAttendanceStatus.Interested),
            });
        }

        // ── POST /api/events ─────────────────────────────────────────────────────
        [HttpPost]
        [Authorize(Policy = "ApiAuth")]
        [EnableRateLimiting("content-write")]
        public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
        {
            var userId = CurrentUserId!;
            if (!User.IsInRole(GlobalConstants.Roles.Organizer) && !IsAdmin)
                return Forbid();

            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Address) || string.IsNullOrWhiteSpace(request.City))
                return BadRequest(new { error = "Попълни всички задължителни полета." });
            if (!request.OrganizerProfileId.HasValue)
                return BadRequest(new { error = "Избери public page. Събитията се публикуват само през публична страница." });
            var profile = await _db.OrganizerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.OrganizerProfileId.Value && p.OwnerId == userId && p.IsActive);
            if (profile == null) return BadRequest(new { error = "Невалидна public page." });

            if (!Enum.TryParse<EventGenre>(request.Genre, true, out var genre))
                return BadRequest(new { error = "Невалиден жанр." });

            if (request.StartTime >= request.EndTime)
                return BadRequest(new { error = "Началото трябва да е преди края." });

            var ticketingMode = ParseTicketingMode(request.TicketingMode);
            if (ticketingMode != EventTicketingMode.GeneralAdmission)
            {
                var layoutOk = request.VenueLayoutId.HasValue && await _db.VenueLayouts.AnyAsync(l => l.Id == request.VenueLayoutId.Value && l.Status != VenueLayoutStatus.Archived && (IsAdmin || l.OrganizerId == userId));
                if (!layoutOk) return BadRequest(new { error = "Избери валиден layout." });
                if (request.LayoutTicketSections == null || request.LayoutTicketSections.Count == 0)
                    return BadRequest(new { error = "Задай цени по секторите на layout-а." });
            }

            var ev = new Event
            {
                OrganizerId = userId,
                OrganizerProfileId = request.OrganizerProfileId,
                BusinessWorkspaceId = request.BusinessWorkspaceId ?? profile.BusinessWorkspaceId,
                Title = request.Title.Trim(),
                Description = request.Description?.Trim(),
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Genre = genre,
                Address = request.Address.Trim(),
                City = request.City.Trim(),
                ImageUrl = request.ImageUrl?.Trim(),
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                TicketingMode = ticketingMode,
                VenueLayoutId = ticketingMode == EventTicketingMode.GeneralAdmission ? null : request.VenueLayoutId,
                IsApproved = false,
            };

            _db.Events.Add(ev);
            await _db.SaveChangesAsync();
            await UpsertSeriesAsync(ev, request, userId);
            await EnsureSeatInventoriesForEventAsync(ev);
            await CreateInitialLayoutTicketsAsync(ev, request);

            return Ok(new { id = ev.Id, title = ev.Title, isApproved = ev.IsApproved });
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "ApiAuth")]
        [EnableRateLimiting("content-write")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateEventRequest request)
        {
            var userId = CurrentUserId!;
            var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null) return NotFound();
            if (!IsAdmin && ev.OrganizerId != userId) return Forbid();

            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Address) || string.IsNullOrWhiteSpace(request.City))
                return BadRequest(new { error = "Попълни всички задължителни полета." });
            if (!request.OrganizerProfileId.HasValue)
                return BadRequest(new { error = "Избери public page. Събитията се публикуват само през публична страница." });
            var profile = await _db.OrganizerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.OrganizerProfileId.Value && (p.OwnerId == userId || IsAdmin) && p.IsActive);
            if (profile == null) return BadRequest(new { error = "Невалидна public page." });

            if (!Enum.TryParse<EventGenre>(request.Genre, true, out var genre))
                return BadRequest(new { error = "Невалиден жанр." });

            if (request.StartTime >= request.EndTime)
                return BadRequest(new { error = "Началото трябва да е преди края." });

            var requestedMode = ParseTicketingMode(request.TicketingMode);
            if (requestedMode != ev.TicketingMode)
            {
                var hasSoldTickets = await _db.UserTickets
                    .AnyAsync(ut => ut.Ticket.EventId == ev.Id);
                if (hasSoldTickets)
                    return BadRequest(new { error = "Не може да се смени типът билети, докато има продадени билети." });
            }

            var newLayoutId = requestedMode == EventTicketingMode.GeneralAdmission ? null : request.VenueLayoutId;
            var newBusinessWorkspaceId = request.BusinessWorkspaceId ?? profile.BusinessWorkspaceId;

            // For approved events, non-admin edits queue a change request
            // instead of mutating the live record. The admin reviews the
            // diff and applies it via /api/admin/event-changes/{id}/approve.
            if (!IsAdmin && ev.IsApproved)
            {
                var changeJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Title = request.Title.Trim(),
                    Description = request.Description?.Trim(),
                    City = request.City.Trim(),
                    Address = request.Address.Trim(),
                    ImageUrl = request.ImageUrl?.Trim(),
                    OrganizerProfileId = request.OrganizerProfileId,
                    BusinessWorkspaceId = newBusinessWorkspaceId,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    Genre = request.Genre,
                    TicketingMode = requestedMode.ToString(),
                    VenueLayoutId = newLayoutId,
                });

                // Replace any earlier pending request from the same organizer
                // so the admin queue doesn't pile up duplicates.
                var existing = await _db.EventChangeRequests
                    .Where(r => r.EventId == ev.Id && r.OrganizerId == userId && r.Status == EventChangeRequestStatus.Pending)
                    .ToListAsync();
                if (existing.Count > 0) _db.EventChangeRequests.RemoveRange(existing);

                _db.EventChangeRequests.Add(new EventChangeRequest
                {
                    EventId = ev.Id,
                    OrganizerId = userId,
                    ChangeJson = changeJson,
                    Status = EventChangeRequestStatus.Pending,
                    SubmittedAt = DateTime.UtcNow,
                });
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    id = ev.Id,
                    title = ev.Title,
                    isApproved = ev.IsApproved,
                    hasPendingChanges = true,
                });
            }

            ev.Title = request.Title.Trim();
            ev.Description = request.Description?.Trim();
            ev.StartTime = request.StartTime;
            ev.EndTime = request.EndTime;
            ev.Genre = genre;
            ev.Address = request.Address.Trim();
            ev.City = request.City.Trim();
            ev.ImageUrl = request.ImageUrl?.Trim();
            ev.OrganizerProfileId = request.OrganizerProfileId;
            ev.BusinessWorkspaceId = newBusinessWorkspaceId;
            ev.Latitude = request.Latitude;
            ev.Longitude = request.Longitude;
            ev.TicketingMode = requestedMode;
            ev.VenueLayoutId = newLayoutId;

            await _db.SaveChangesAsync();
            await UpsertSeriesAsync(ev, request, userId);
            return Ok(new { id = ev.Id, title = ev.Title, isApproved = ev.IsApproved });
        }

        [HttpGet("layout-ticket-sections/{layoutId:int}")]
        [Authorize(Policy = "ApiAuth")]
        public async Task<IActionResult> LayoutTicketSections(int layoutId)
        {
            var userId = CurrentUserId!;
            var layoutAvailable = await _db.VenueLayouts
                .AsNoTracking()
                .AnyAsync(l => l.Id == layoutId && l.Status != VenueLayoutStatus.Archived && (IsAdmin || l.OrganizerId == userId));
            if (!layoutAvailable) return NotFound();

            var seats = await _db.Seats
                .AsNoTracking()
                .Where(s => s.VenueLayoutId == layoutId && s.Status == LayoutSeatStatus.Active)
                .Select(s => new
                {
                    s.SectionId,
                    SectionName = s.Section.Name,
                    SectionColorHex = s.Section.ColorHex,
                    SeatColorHex = s.ColorHex,
                })
                .ToListAsync();

            var sections = seats
                .GroupBy(s => NormalizeColorHex(s.SeatColorHex, NormalizeColorHex(s.SectionColorHex, "#2456ff")))
                .Select(group =>
                {
                    var sectionIds = group.Select(s => s.SectionId).Distinct().OrderBy(id => id).ToList();
                    var names = group.Select(s => s.SectionName).Distinct().OrderBy(name => name).ToList();
                    var isSingleDefaultSectionGroup = sectionIds.Count == 1 &&
                        group.All(s => NormalizeColorHex(s.SectionColorHex, "#2456ff") == group.Key);
                    var label = names.Count == 1
                        ? (isSingleDefaultSectionGroup ? names[0] : $"{names[0]} · ценова група")
                        : $"Ценова група · {names.Count} секции";
                    return new LayoutTicketSectionRequest
                    {
                        SectionId = sectionIds.First(),
                        SectionIds = sectionIds,
                        GroupKey = "color:" + group.Key,
                        SectionName = label,
                        ColorHex = group.Key,
                        SeatsCount = group.Count(),
                        Price = 0m,
                        RequiresAttendeeNames = false,
                    };
                })
                .OrderBy(s => s.SectionName)
                .ToList();

            foreach (var duplicateGroup in sections.GroupBy(s => s.SectionName).Where(g => g.Count() > 1))
            {
                var index = 1;
                foreach (var section in duplicateGroup.OrderBy(s => s.ColorHex))
                {
                    section.SectionName = $"{section.SectionName} {index}";
                    index++;
                }
            }

            return Ok(sections);
        }

        [HttpPost("generate-description")]
        [Authorize(Policy = "ApiAuth")]
        [EnableRateLimiting("ai-light")]
        public async Task<IActionResult> GenerateDescription([FromBody] GenerateDescriptionRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest(new { ok = false, error = "Въведи заглавие първо." });
            if (!_ai.IsEnabled)
                return StatusCode(503, new { ok = false, error = "AI не е конфигуриран." });

            var description = await _ai.GenerateEventDescriptionAsync(
                request.Title,
                request.City,
                request.Genre,
                request.Hints,
                request.Lang,
                cancellationToken);

            return string.IsNullOrWhiteSpace(description)
                ? StatusCode(503, new { ok = false, error = _ai.LastStatusDetail ?? "AI не върна текст." })
                : Ok(new { ok = true, description });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "ApiAuth")]
        [EnableRateLimiting("content-write")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId!;
            var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            if (ev == null) return NotFound();
            if (!IsAdmin && ev.OrganizerId != userId) return Forbid();

            var result = await _eventDeletion.DeleteEventAsync(id, preservePaidTickets: true, cancellationToken);
            if (result.Deleted) return Ok(new { deleted = true });
            if (result.SkippedReason == "paid_tickets")
                return Conflict(new { error = "Събитието има платени билети и не може да бъде изтрито." });

            return NotFound();
        }

        // ── GET /api/events/{id}/comments ────────────────────────────────────────
        [HttpGet("{id:int}/comments")]
        [EnableRateLimiting("public-read")]
        public async Task<IActionResult> GetComments(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = CurrentUserId;
            var comments = await _db.EventComments
                .AsNoTracking()
                .Where(c => c.EventId == id && c.ParentCommentId == null)
                .Include(c => c.User)
                .Include(c => c.Likes)
                .Include(c => c.Replies).ThenInclude(r => r.User)
                .Include(c => c.Replies).ThenInclude(r => r.Likes)
                .OrderByDescending(c => c.Likes.Count)
                .ThenByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(comments.Select(c => MapComment(c, userId)));
        }

        // ── POST /api/events/{id}/comments ───────────────────────────────────────
        [HttpPost("{id:int}/comments")]
        [Authorize(Policy = "ApiAuth")]
        [EnableRateLimiting("content-write")]
        public async Task<IActionResult> AddComment(int id, [FromBody] CommentRequest request)
        {
            var userId = CurrentUserId!;
            if (string.IsNullOrWhiteSpace(request.Content))
                return BadRequest(new { error = "Коментарът не може да е празен." });

            if (!await _db.Events.AnyAsync(e => e.Id == id)) return NotFound();

            int? parentCommentId = null;
            if (request.ParentCommentId.HasValue)
            {
                var parent = await _db.EventComments
                    .AsNoTracking()
                    .Where(c => c.Id == request.ParentCommentId.Value && c.EventId == id)
                    .Select(c => new { c.Id, c.ParentCommentId })
                    .FirstOrDefaultAsync();
                if (parent == null)
                    return BadRequest(new { error = "Невалиден родителски коментар." });
                // Flatten nested replies onto the root comment so the tree stays one level deep.
                parentCommentId = parent.ParentCommentId ?? parent.Id;
            }

            var comment = new EventComment
            {
                EventId = id,
                UserId = userId,
                Content = request.Content.Trim(),
                ParentCommentId = parentCommentId,
                CreatedAt = DateTime.UtcNow,
            };
            _db.EventComments.Add(comment);
            await _db.SaveChangesAsync();

            var user = await _userManager.FindByIdAsync(userId);
            return Ok(new
            {
                id = comment.Id,
                userId = comment.UserId,
                userName = user?.UserName ?? "",
                authorImageUrl = user?.ProfileImageUrl,
                content = comment.Content,
                createdAt = comment.CreatedAt,
                likesCount = 0,
                currentUserLiked = false,
                canDelete = true,
                replies = Array.Empty<object>(),
            });
        }

        // ── DELETE /api/events/{id}/comments/{commentId} ─────────────────────────
        [HttpDelete("{id:int}/comments/{commentId:int}")]
        [Authorize(Policy = "ApiAuth")]
        [EnableRateLimiting("content-write")]
        public async Task<IActionResult> DeleteComment(int id, int commentId)
        {
            var userId = CurrentUserId!;
            var comment = await _db.EventComments.FindAsync(commentId);
            if (comment == null || comment.EventId != id) return NotFound();
            if (comment.UserId != userId && !IsAdmin) return Forbid();

            _db.EventComments.Remove(comment);
            await _db.SaveChangesAsync();
            return Ok(new { deleted = true });
        }

        // ── POST /api/events/{id}/comments/{commentId}/like ──────────────────────
        [HttpPost("{id:int}/comments/{commentId:int}/like")]
        [Authorize(Policy = "ApiAuth")]
        [EnableRateLimiting("interactions")]
        public async Task<IActionResult> LikeComment(int id, int commentId)
        {
            var userId = CurrentUserId!;
            var comment = await _db.EventComments.FirstOrDefaultAsync(c => c.Id == commentId && c.EventId == id);
            if (comment == null) return NotFound();

            var exists = await _db.EventCommentLikes.AnyAsync(l => l.EventCommentId == commentId && l.UserId == userId);
            if (!exists)
            {
                _db.EventCommentLikes.Add(new EventCommentLike { EventCommentId = commentId, UserId = userId, CreatedAt = DateTime.UtcNow });
                await _db.SaveChangesAsync();
            }
            var count = await _db.EventCommentLikes.CountAsync(l => l.EventCommentId == commentId);
            return Ok(new { likesCount = count, currentUserLiked = true });
        }

        // ── DELETE /api/events/{id}/comments/{commentId}/like ────────────────────
        [HttpDelete("{id:int}/comments/{commentId:int}/like")]
        [Authorize(Policy = "ApiAuth")]
        [EnableRateLimiting("interactions")]
        public async Task<IActionResult> UnlikeComment(int id, int commentId)
        {
            var userId = CurrentUserId!;
            var like = await _db.EventCommentLikes.FirstOrDefaultAsync(l => l.EventCommentId == commentId && l.UserId == userId);
            if (like != null) { _db.EventCommentLikes.Remove(like); await _db.SaveChangesAsync(); }
            var count = await _db.EventCommentLikes.CountAsync(l => l.EventCommentId == commentId);
            return Ok(new { likesCount = count, currentUserLiked = false });
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static object MapToCard(Event e, string? userId) => new
        {
            id = e.Id,
            title = e.Title,
            description = e.Description,
            startTime = e.StartTime,
            endTime = e.EndTime,
            genre = e.Genre.ToString(),
            imageUrl = e.ImageUrl,
            address = e.Address,
            city = e.City,
            latitude = e.Latitude,
            longitude = e.Longitude,
            organizerName = OrganizerDisplayName(e),
            organizerProfileId = e.OrganizerProfileId,
            likesCount = e.Likes.Count,
            savesCount = e.Saves.Count,
            goingCount = e.Attendances.Count(a => a.Status == EventAttendanceStatus.Going),
            interestedCount = e.Attendances.Count(a => a.Status == EventAttendanceStatus.Interested),
            isLiked = userId != null && e.Likes.Any(l => l.UserId == userId),
            isSaved = userId != null && e.Saves.Any(s => s.UserId == userId),
            userAttendanceStatus = userId != null
                ? e.Attendances.FirstOrDefault(a => a.UserId == userId)?.Status.ToString()
                : null,
        };

        private static object MapComment(EventComment c, string? userId) => new
        {
            id = c.Id,
            userId = c.UserId,
            userName = c.User?.UserName ?? "",
            authorImageUrl = c.User?.ProfileImageUrl,
            content = c.Content,
            createdAt = c.CreatedAt,
            likesCount = c.Likes.Count,
            currentUserLiked = userId != null && c.Likes.Any(l => l.UserId == userId),
            canDelete = userId == c.UserId,
            replies = (c.Replies ?? new List<EventComment>()).Select(r => new
            {
                id = r.Id,
                userId = r.UserId,
                userName = r.User?.UserName ?? "",
                authorImageUrl = r.User?.ProfileImageUrl,
                content = r.Content,
                createdAt = r.CreatedAt,
                likesCount = r.Likes?.Count ?? 0,
                currentUserLiked = userId != null && (r.Likes?.Any(l => l.UserId == userId) ?? false),
                canDelete = userId == r.UserId,
                replies = Array.Empty<object>(),
            }).ToArray(),
        };

        private static string OrganizerDisplayName(Event e)
        {
            return e.OrganizerProfile?.DisplayName ?? "";
        }

        private static EventTicketingMode ParseTicketingMode(string? value)
        {
            return Enum.TryParse<EventTicketingMode>(value, true, out var mode)
                ? mode
                : EventTicketingMode.GeneralAdmission;
        }

        // Escapes the LIKE wildcards so a user typing "50% off" doesn't get a wildcard match.
        private static string EscapeLikePattern(string value)
        {
            return value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal);
        }

        private static string NormalizeColorHex(string? value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            var color = value.Trim();
            if (!color.StartsWith("#", StringComparison.Ordinal)) color = "#" + color;
            return System.Text.RegularExpressions.Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$") ? color.ToLowerInvariant() : fallback;
        }

        private async Task UpsertSeriesAsync(Event ev, CreateEventRequest request, string userId)
        {
            if (!Enum.TryParse<EventRecurrenceType>(request.RecurrenceType, true, out var recurrenceType) ||
                recurrenceType == EventRecurrenceType.None)
            {
                return;
            }

            var series = await _db.EventSeries
                .Include(s => s.Occurrences)
                .FirstOrDefaultAsync(s => s.EventId == ev.Id);

            if (series == null)
            {
                series = new EventSeries
                {
                    EventId = ev.Id,
                    OrganizerId = userId,
                };
                _db.EventSeries.Add(series);
            }

            series.Title = ev.Title;
            series.Description = ev.Description;
            series.Category = ev.Genre;
            series.Location = ev.Address;
            series.City = ev.City;
            series.ImageUrl = ev.ImageUrl;
            series.RecurrenceType = recurrenceType;
            series.Interval = Math.Clamp(request.RecurrenceInterval ?? 1, 1, 365);
            series.DaysOfWeek = request.DaysOfWeek == null ? null : string.Join(",", request.DaysOfWeek.Distinct());
            series.OccurrenceDisplayMode = EventOccurrenceDisplayMode.ShowAllDates;
            series.StartDate = (request.RecurrenceStartDate ?? ev.StartTime).Date;
            series.EndDate = (request.RecurrenceEndDate ?? ev.EndTime).Date;
            series.StartTime = request.RecurrenceStartTime ?? ev.StartTime.TimeOfDay;
            series.EndTime = request.RecurrenceEndTime ?? ev.EndTime.TimeOfDay;
            series.TimeZone = string.IsNullOrWhiteSpace(request.TimeZone) ? "Europe/Sofia" : request.TimeZone;
            series.Status = EventSeriesStatus.Published;
            series.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await _recurringEvents.RegenerateOccurrencesAsync(series, RecurringEditScope.EntireSeries);
        }

        private async Task EnsureSeatInventoriesForEventAsync(Event ev)
        {
            if (ev.VenueLayoutId == null || ev.TicketingMode == EventTicketingMode.GeneralAdmission) return;
            var seriesId = await _db.EventSeries.Where(s => s.EventId == ev.Id).Select(s => (int?)s.Id).FirstOrDefaultAsync();
            if (seriesId.HasValue)
            {
                var occurrenceIds = await _db.EventOccurrences.Where(o => o.EventSeriesId == seriesId.Value).Select(o => o.Id).ToListAsync();
                foreach (var occurrenceId in occurrenceIds)
                {
                    await _layouts.EnsureInventoryAsync(ev.Id, occurrenceId, ev.VenueLayoutId.Value);
                }
                return;
            }
            await _layouts.EnsureInventoryAsync(ev.Id, null, ev.VenueLayoutId.Value);
        }

        private async Task CreateInitialLayoutTicketsAsync(Event ev, CreateEventRequest request)
        {
            if (ev.VenueLayoutId == null || ev.TicketingMode == EventTicketingMode.GeneralAdmission || request.LayoutTicketSections == null || request.LayoutTicketSections.Count == 0)
                return;

            var requestedSectionIds = request.LayoutTicketSections
                .SelectMany(s => s.SectionIds != null && s.SectionIds.Any() ? s.SectionIds : new List<int> { s.SectionId })
                .ToHashSet();

            var actualSeats = await _db.Seats
                .AsNoTracking()
                .Where(s => s.VenueLayoutId == ev.VenueLayoutId.Value && requestedSectionIds.Contains(s.SectionId) && s.Status == LayoutSeatStatus.Active)
                .Select(s => new
                {
                    s.SectionId,
                    SectionName = s.Section.Name,
                    SectionColorHex = s.Section.ColorHex,
                    SeatColorHex = s.ColorHex,
                })
                .ToListAsync();

            foreach (var section in request.LayoutTicketSections)
            {
                if (section.Price < 0) continue;
                var sectionIds = section.SectionIds != null && section.SectionIds.Any()
                    ? section.SectionIds.ToHashSet()
                    : new HashSet<int> { section.SectionId };
                var requestedColor = NormalizeColorHex(section.ColorHex, "#2456ff");
                var matchingSeats = actualSeats
                    .Where(s => sectionIds.Contains(s.SectionId) &&
                        NormalizeColorHex(s.SeatColorHex, NormalizeColorHex(s.SectionColorHex, "#2456ff")) == requestedColor)
                    .ToList();
                if (matchingSeats.Count() == 0) continue;

                var sectionNames = matchingSeats.Select(s => s.SectionName).Distinct().OrderBy(name => name).ToList();
                var ticketName = string.IsNullOrWhiteSpace(section.SectionName)
                    ? (sectionNames.Count() == 1 ? $"{sectionNames[0]} · ценова група" : $"Ценова група · {sectionNames.Count} секции")
                    : section.SectionName.Trim();
                var ticket = new Ticket
                {
                    EventId = ev.Id,
                    Name = ticketName,
                    Description = sectionNames.Count() == 1 ? $"Секция {sectionNames[0]}" : $"Ценова група: {string.Join(", ", sectionNames)}",
                    Price = section.Price,
                    QuantityTotal = matchingSeats.Count(),
                    QuantityRemaining = matchingSeats.Count(),
                    IsActive = true,
                    RequiresAttendeeNames = section.RequiresAttendeeNames,
                };
                foreach (var sectionId in matchingSeats.Select(s => s.SectionId).Distinct())
                {
                    ticket.SectionPrices.Add(new TicketSectionPrice { SectionId = sectionId, ColorHex = requestedColor, Price = section.Price });
                }
                _db.Tickets.Add(ticket);
            }
            await _db.SaveChangesAsync();
        }

        // ── Request DTOs ─────────────────────────────────────────────────────────

        public class AttendRequest { public string Status { get; set; } = "Going"; }
        public class CommentRequest
        {
            public string Content { get; set; } = "";
            public int? ParentCommentId { get; set; }
        }
        public class CreateEventRequest
        {
            public string Title { get; set; } = "";
            public string? Description { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public string Genre { get; set; } = "Other";
            public string Address { get; set; } = "";
            public string City { get; set; } = "";
            public string? ImageUrl { get; set; }
            public int? OrganizerProfileId { get; set; }
            public int? BusinessWorkspaceId { get; set; }
            public string? TicketingMode { get; set; }
            public int? VenueLayoutId { get; set; }
            public List<LayoutTicketSectionRequest>? LayoutTicketSections { get; set; }
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
            public string? RecurrenceType { get; set; }
            public int? RecurrenceInterval { get; set; }
            public string[]? DaysOfWeek { get; set; }
            public DateTime? RecurrenceStartDate { get; set; }
            public DateTime? RecurrenceEndDate { get; set; }
            public TimeSpan? RecurrenceStartTime { get; set; }
            public TimeSpan? RecurrenceEndTime { get; set; }
            public string? TimeZone { get; set; }
        }

        public class GenerateDescriptionRequest
        {
            public string Title { get; set; } = "";
            public string? City { get; set; }
            public string? Genre { get; set; }
            public string? Hints { get; set; }
            public string? Lang { get; set; }
        }

        public class LayoutTicketSectionRequest
        {
            public int SectionId { get; set; }
            public List<int>? SectionIds { get; set; }
            public string? GroupKey { get; set; }
            public string SectionName { get; set; } = "";
            public string ColorHex { get; set; } = "#2456ff";
            public int SeatsCount { get; set; }
            public decimal Price { get; set; }
            public bool RequiresAttendeeNames { get; set; }
        }
    }
}
