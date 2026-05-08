using EventsApp.Common;
using EventsApp.Data;
using EventsApp.Models;
using EventsApp.ViewModels.Preferences;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Controllers
{
    [Authorize]
    public class PreferencesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public PreferencesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;
            var prefs = await _db.UserPreferences.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId);

            if (prefs == null) return View("Index", (PreferencesViewModel?)null);

            return View(new PreferencesViewModel
            {
                PreferredGenres = prefs.PreferredGenres.ToList(),
                PreferredCity = CityCoordinates.GetCanonicalName(prefs.PreferredCity) ?? prefs.PreferredCity,
                MinAge = prefs.MinAge,
                MaxDistanceKm = prefs.MaxDistanceKm,
            });
        }

        public async Task<IActionResult> Edit(int? welcome = null)
        {
            var userId = _userManager.GetUserId(User)!;
            var prefs = await _db.UserPreferences.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId);

            var vm = prefs == null
                ? new PreferencesViewModel()
                : new PreferencesViewModel
                {
                    PreferredGenres = prefs.PreferredGenres.ToList(),
                    PreferredCity = CityCoordinates.GetCanonicalName(prefs.PreferredCity) ?? prefs.PreferredCity,
                    MinAge = prefs.MinAge,
                    MaxDistanceKm = prefs.MaxDistanceKm,
                };

            ViewBag.IsWelcome = welcome == 1;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PreferencesViewModel input, int? welcome = null)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.IsWelcome = welcome == 1;
                return View(input);
            }

            var userId = _userManager.GetUserId(User)!;
            var prefs = await _db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId);

            var genres = (input.PreferredGenres ?? new List<EventGenre>())
                .Distinct()
                .ToList();
            var preferredCity = CityCoordinates.GetCanonicalName(input.PreferredCity);

            if (prefs == null)
            {
                var created = new UserPreferences
                {
                    UserId = userId,
                    PreferredCity = preferredCity,
                    MinAge = input.MinAge,
                    MaxDistanceKm = input.MaxDistanceKm,
                };
                created.PreferredGenres = genres;
                _db.UserPreferences.Add(created);
            }
            else
            {
                prefs.PreferredGenres = genres;
                prefs.PreferredCity = preferredCity;
                prefs.MinAge = input.MinAge;
                prefs.MaxDistanceKm = input.MaxDistanceKm;
            }

            await _db.SaveChangesAsync();

            if (welcome == 1)
            {
                TempData["StatusMessage"] = "Готово! Ето събития, които може да ти харесат.";
                return RedirectToAction("Recommended", "Events");
            }

            TempData["StatusMessage"] = "Preferences saved.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete()
        {
            var userId = _userManager.GetUserId(User)!;
            var prefs = await _db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
            if (prefs != null)
            {
                _db.UserPreferences.Remove(prefs);
                await _db.SaveChangesAsync();
            }
            TempData["StatusMessage"] = "Preferences cleared.";
            return RedirectToAction(nameof(Index));
        }
    }
}
