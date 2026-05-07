using System.Security.Cryptography;
using EventsApp.Common;
using EventsApp.Data;
using EventsApp.Models;
using EventsApp.Services.AI;
using EventsApp.ViewModels.Wrapped;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Controllers
{
    public class AiPlanController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IAiSearchService _ai;
        private readonly UserManager<ApplicationUser> _userManager;

        public AiPlanController(ApplicationDbContext db, IAiSearchService ai, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _ai = ai;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new DayPlanViewModel();

            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId))
            {
                var today = DateTime.UtcNow.Date;
                vm.MyPlans = await LoadActivePlansAsync(userId, today);
            }

            return View(vm);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(string description)
        {
            var userId = _userManager.GetUserId(User)!;

            if (string.IsNullOrWhiteSpace(description))
            {
                TempData["AiPlanError"] = "Напиши какво искаш — напр. 'утре в София с приятели, нещо живо след 20:00'.";
                return RedirectToAction(nameof(Index));
            }

            if (!_ai.IsEnabled)
            {
                TempData["AiPlanError"] = "AI асистентът не е конфигуриран.";
                return RedirectToAction(nameof(Index));
            }

            var knownCities = await _db.Events
                .AsNoTracking()
                .Where(e => e.IsApproved)
                .Select(e => e.City)
                .Distinct()
                .ToListAsync();

            var intent = await _ai.ParseDayPlanRequestAsync(description, knownCities);
            if (intent == null || string.IsNullOrWhiteSpace(intent.City) || !intent.Date.HasValue)
            {
                TempData["AiPlanError"] = "Не разбрах заявката. Опитай със: 'град + ден + настроение'.";
                return RedirectToAction(nameof(Index));
            }

            var dayStart = intent.Date.Value.Date;
            var dayEnd = dayStart.AddDays(1);
            var cityVariants = CityCoordinates.GetEquivalentNames(intent.City);

            var rawEvents = await _db.Events
                .AsNoTracking()
                .Where(e => e.IsApproved
                    && cityVariants.Contains(e.City)
                    && e.StartTime >= dayStart
                    && e.StartTime < dayEnd)
                .OrderBy(e => e.StartTime)
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.Genre,
                    e.Address,
                    e.City,
                    e.StartTime,
                    e.EndTime,
                })
                .ToListAsync();

            var candidates = rawEvents.Select(e => new DayPlanEventCandidate
            {
                Id = e.Id,
                Title = e.Title,
                Genre = e.Genre.GetDisplayName(),
                Address = string.IsNullOrWhiteSpace(e.Address) ? e.City : e.Address,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
            }).ToList();

            var timeline = await _ai.GenerateDayPlanTimelineAsync(intent, candidates);
            if (timeline == null)
            {
                TempData["AiPlanError"] = "AI не върна валиден план. Опитай пак.";
                return RedirectToAction(nameof(Index));
            }

            var validIds = candidates.Select(c => c.Id).ToHashSet();

            var plan = new DayPlan
            {
                UserId = userId,
                City = intent.City,
                PlannedFor = dayStart,
                UserRequest = description.Trim().Length > 500 ? description.Trim()[..500] : description.Trim(),
                Vibe = Truncate(intent.Vibe, 120),
                Title = Truncate(timeline.Title, 160),
                Intro = Truncate(timeline.Intro, 800),
            };

            int order = 0;
            foreach (var slot in timeline.Slots)
            {
                if (string.IsNullOrWhiteSpace(slot.Title)) continue;

                var kind = slot.Slot?.ToLowerInvariant() switch
                {
                    "before" => DayPlanSlotKind.Before,
                    "after" => DayPlanSlotKind.After,
                    _ => DayPlanSlotKind.Main,
                };

                var item = new DayPlanItem
                {
                    Slot = kind,
                    Order = order++,
                    StartTime = Truncate(slot.StartTime, 16),
                    EndTime = Truncate(slot.EndTime, 16),
                    Title = Truncate(slot.Title, 160) ?? "",
                    Description = Truncate(slot.Description, 500),
                    EventId = (slot.EventId.HasValue && validIds.Contains(slot.EventId.Value)) ? slot.EventId : null,
                };
                plan.Items.Add(item);
            }

            _db.DayPlans.Add(plan);
            await _db.SaveChangesAsync();

            TempData["AiPlanCreatedId"] = plan.Id;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var plan = await _db.DayPlans.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if (plan != null)
            {
                _db.DayPlans.Remove(plan);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Share(int id)
        {
            var userId = _userManager.GetUserId(User);
            var plan = await _db.DayPlans.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if (plan == null) return NotFound();

            if (string.IsNullOrEmpty(plan.ShareToken))
            {
                plan.ShareToken = GenerateShareToken();
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("AiPlan/View/{token}")]
        public async Task<IActionResult> ViewShared(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return NotFound();

            var plan = await _db.DayPlans
                .AsNoTracking()
                .Include(p => p.Items)
                    .ThenInclude(i => i.Event)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.ShareToken == token);

            if (plan == null) return NotFound();

            return View("Shared", plan);
        }

        private async Task<List<DayPlan>> LoadActivePlansAsync(string userId, DateTime todayUtc)
        {
            return await _db.DayPlans
                .AsNoTracking()
                .Where(p => p.UserId == userId && p.PlannedFor >= todayUtc)
                .OrderBy(p => p.PlannedFor)
                .Include(p => p.Items.OrderBy(i => i.Order))
                    .ThenInclude(i => i.Event)
                .ToListAsync();
        }

        private static string GenerateShareToken()
        {
            Span<byte> bytes = stackalloc byte[16];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string? Truncate(string? s, int max)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Length <= max ? s : s[..max];
        }
    }
}
