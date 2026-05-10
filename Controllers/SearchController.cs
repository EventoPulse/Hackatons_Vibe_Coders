using EventsApp.Common;
using EventsApp.Data;
using EventsApp.Models;
using EventsApp.Services.AI;
using EventsApp.ViewModels.Events;
using EventsApp.ViewModels.Home;
using EventsApp.ViewModels.Posts;
using EventsApp.ViewModels.Search;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Controllers
{
    [EnableRateLimiting("public-read")]
    public class SearchController : Controller
    {
        private const int ResultsPerSection = 12;
        private const int MaxAiQueryLength = 180;

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAiSearchService _ai;

        public SearchController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IAiSearchService ai)
        {
            _db = db;
            _userManager = userManager;
            _ai = ai;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? q, CancellationToken cancellationToken)
        {
            var vm = new SearchResultsViewModel { Query = q };

            if (string.IsNullOrWhiteSpace(q))
            {
                ApplyHint(vm, intentExtracted: false);
                return View(vm);
            }

            var term = q.Trim();
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole(GlobalConstants.Roles.Admin);

            var local = LocalEventSearchInterpreter.Parse(term, DateTime.UtcNow);
            AiSearchIntent? intent = local.Intent;
            var aiWasCalled = false;
            if (_ai.IsEnabled && !local.HasStrongIntent && local.ShouldAskAi)
            {
                var aiTerm = term.Length > MaxAiQueryLength ? term[..MaxAiQueryLength] : term;
                var aiIntent = await _ai.InterpretAsync(aiTerm, cancellationToken);
                if (aiIntent != null)
                {
                    intent = LocalEventSearchInterpreter.Merge(intent, aiIntent);
                    aiWasCalled = true;
                }
            }

            var keyword = ResolveKeyword(term, intent);

            var visibleEventsQuery = _db.Events
                .AsNoTracking()
                .Where(e => isAdmin || e.IsApproved);

            var eventsQuery = visibleEventsQuery;

            var cityFilters = GetCityFilters(intent);
            if (cityFilters.Count > 0)
            {
                eventsQuery = eventsQuery.Where(e => cityFilters.Contains(e.City));
            }

            var genreFilters = GetGenreFilters(intent);
            if (genreFilters.Count > 0)
            {
                eventsQuery = ApplyGenreSearch(eventsQuery, genreFilters);
            }

            if (intent?.DateFrom.HasValue == true)
            {
                var df = intent.DateFrom.Value;
                eventsQuery = eventsQuery.Where(e => e.StartTime >= df);
            }

            if (intent?.DateTo.HasValue == true)
            {
                var dt = intent.DateTo.Value.AddDays(1);
                eventsQuery = eventsQuery.Where(e => e.StartTime < dt);
            }

            var keywordTerms = BuildKeywordTerms(keyword, intent);
            if (keywordTerms.Count > 0)
            {
                eventsQuery = ApplyKeywordSearch(eventsQuery, keywordTerms);
            }

            var matchedEvents = await ProjectEventCards(eventsQuery, userId)
                .OrderBy(e => e.StartTime)
                .Take(ResultsPerSection)
                .ToListAsync(cancellationToken);

            var matchedPosts = await ProjectPostCards(keywordTerms, userId)
                .OrderByDescending(p => p.CreatedAt)
                .Take(ResultsPerSection)
                .ToListAsync(cancellationToken);

            vm.Posts = matchedPosts;

            if (matchedEvents.Count == 0)
            {
                vm.ShowingFallbackEvents = true;
                vm.SearchMessageLevel = "warning";
                vm.SearchMessage = $"No event matches were found for \"{term}\". Showing all available events instead.";

                vm.Events = await ProjectEventCards(visibleEventsQuery, userId)
                    .OrderBy(e => e.StartTime)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                vm.Events = matchedEvents;
            }

            vm.MapMarkers = BuildMarkers(vm.Events);

            if (intent != null)
            {
                vm.AiUsed = true;
                vm.InterpretedCity = intent.City;
                vm.InterpretedKeyword = intent.Keyword;
                vm.InterpretedGenre = intent.Genre;
                vm.InterpretedDateFrom = intent.DateFrom;
                vm.InterpretedDateTo = intent.DateTo;
                vm.InterpretedNearMe = intent.NearMe;
            }

            ApplyHint(vm, intent != null, aiWasCalled);
            return View(vm);
        }

        private IQueryable<EventCardViewModel> ProjectEventCards(IQueryable<Event> query, string? userId)
        {
            return query.Select(e => new EventCardViewModel
            {
                Id = e.Id,
                Title = e.Title,
                ImageUrl = e.ImageUrl,
                Address = e.Address,
                City = e.City,
                StartTime = e.StartTime,
                Genre = e.Genre,
                GenreTags = e.GenreTags,
                IsApproved = e.IsApproved,
                OrganizerId = e.OrganizerId,
                OrganizerProfileId = e.OrganizerProfileId,
                OrganizerName = e.OrganizerProfile != null ? e.OrganizerProfile.DisplayName : "Public page",
                LikesCount = e.Likes.Count,
                CommentsCount = e.Comments.Count,
                SavesCount = e.Saves.Count,
                GoingCount = e.Attendances.Count(a => a.Status == EventAttendanceStatus.Going),
                InterestedCount = e.Attendances.Count(a => a.Status == EventAttendanceStatus.Interested),
                CurrentUserLiked = userId != null && e.Likes.Any(l => l.UserId == userId),
                CurrentUserSaved = userId != null && e.Saves.Any(s => s.UserId == userId),
                CurrentUserAttendanceStatus = userId == null
                    ? null
                    : e.Attendances
                        .Where(a => a.UserId == userId)
                        .Select(a => (EventAttendanceStatus?)a.Status)
                        .FirstOrDefault(),
                Latitude = e.Latitude,
                Longitude = e.Longitude,
                HasActiveTickets = e.Tickets.Any(t => t.IsActive),
                HasPaidTickets = e.Tickets.Any(t => t.IsActive && t.Price > 0m),
                LowestPaidTicketPrice = e.Tickets
                    .Where(t => t.IsActive && t.Price > 0m)
                    .Min(t => (decimal?)t.Price),
            });
        }

        private IQueryable<PostCardViewModel> ProjectPostCards(IReadOnlyList<string> keywordTerms, string? userId)
        {
            var posts = _db.Posts.AsNoTracking();

            if (keywordTerms.Count == 0)
            {
                posts = posts.Where(_ => false);
            }
            else
            {
                var filtered = posts.Where(_ => false);
                foreach (var term in keywordTerms)
                {
                    var pattern = "%" + EscapeLike(term) + "%";
                    filtered = filtered.Concat(posts.Where(p =>
                        EF.Functions.ILike(p.Content, pattern) ||
                        (p.Event != null && EF.Functions.ILike(p.Event.Title, pattern))));
                }

                posts = filtered.Distinct();
            }

            return posts.Select(p => new PostCardViewModel
            {
                Id = p.Id,
                OrganizerId = p.OrganizerId,
                OrganizerProfileId = p.OrganizerProfileId,
                OrganizerName = p.OrganizerProfile != null
                    ? p.OrganizerProfile.DisplayName
                    : p.Organizer.OrganizerData != null && p.Organizer.OrganizerData.Approved
                    ? "Public page"
                    : p.Organizer.UserName ?? string.Empty,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                EventId = p.EventId,
                EventTitle = p.Event != null ? p.Event.Title : null,
                FirstMediaUrl = p.Images.Select(i => i.ImageUrl).FirstOrDefault(),
                FirstMediaType = p.Images.Select(i => i.MediaType).FirstOrDefault(),
                LikesCount = p.Likes.Count,
                CommentsCount = p.Comments.Count,
                SavesCount = p.Saves.Count,
                CurrentUserLiked = userId != null && p.Likes.Any(l => l.UserId == userId),
                CurrentUserSaved = userId != null && p.Saves.Any(s => s.UserId == userId),
                AuthorImageUrl = p.OrganizerProfile != null && !string.IsNullOrWhiteSpace(p.OrganizerProfile.AvatarImageUrl)
                    ? p.OrganizerProfile.AvatarImageUrl
                    : p.Organizer.OrganizerData != null && p.Organizer.OrganizerData.Approved
                    ? null
                    : p.Organizer.ProfileImageUrl,
                AuthorIsOrganizer = (p.OrganizerProfile != null && p.OrganizerProfile.IsActive && p.OrganizerProfile.IsApproved)
                    || (p.Organizer.OrganizerData != null && p.Organizer.OrganizerData.Approved),
            });
        }

        private void ApplyHint(SearchResultsViewModel vm, bool intentExtracted, bool aiWasCalled = false)
        {
            if (!_ai.IsEnabled)
            {
                vm.AiHintLevel = "info";
                vm.AiHint = "Smart search is using the local parser.";
                return;
            }

            if (intentExtracted && !aiWasCalled)
            {
                vm.AiHintLevel = "success";
                vm.AiHint = "Parsed locally without spending AI tokens.";
                return;
            }

            switch (_ai.LastStatus)
            {
                case AiStatus.MissingProjectId:
                    vm.AiHintLevel = "warning";
                    vm.AiHint = "AI is configured, but the search assistant is not linked. Check the AI settings and restart.";
                    break;
                case AiStatus.ProvisionFailed:
                    vm.AiHintLevel = "danger";
                    vm.AiHint = "AI setup failed: " + (_ai.LastStatusDetail ?? "unknown error");
                    break;
                case AiStatus.CallFailed:
                    vm.AiHintLevel = "danger";
                    vm.AiHint = "AI call failed: " + (_ai.LastStatusDetail ?? "unknown error");
                    break;
                case AiStatus.ParseFailed:
                    vm.AiHintLevel = "warning";
                    vm.AiHint = "AI responded but the answer didn't match the expected JSON shape - falling back to keyword search.";
                    break;
                case AiStatus.Ok when intentExtracted:
                    vm.AiHintLevel = "success";
                    vm.AiHint = "AI parsed your query into structured filters.";
                    break;
                case AiStatus.Ok:
                    vm.AiHintLevel = "info";
                    vm.AiHint = string.IsNullOrWhiteSpace(vm.Query)
                        ? "Smart AI search is on. Try: \"techno tonight in Sofia\" or \"jazz this weekend\"."
                        : "Showing keyword matches (AI couldn't extract filters from this query).";
                    break;
                default:
                    vm.AiHintLevel = "info";
                    vm.AiHint = "Smart AI search is on.";
                    break;
            }
        }

        private static string? ResolveKeyword(string rawQuery, AiSearchIntent? intent)
        {
            if (intent == null)
            {
                return rawQuery;
            }

            if (!string.IsNullOrWhiteSpace(intent.Keyword))
            {
                return intent.Keyword.Trim();
            }

            var hasStructuredFilters =
                !string.IsNullOrWhiteSpace(intent.City) ||
                intent.Cities.Length > 0 ||
                intent.Genre.HasValue ||
                intent.Genres.Length > 0 ||
                intent.DateFrom.HasValue ||
                intent.DateTo.HasValue ||
                intent.NearMe ||
                !string.IsNullOrWhiteSpace(intent.DateIntent) ||
                intent.Latitude.HasValue ||
                intent.Longitude.HasValue;

            return hasStructuredFilters ? null : rawQuery;
        }

        private static IReadOnlyList<string> GetCityFilters(AiSearchIntent? intent)
        {
            if (intent == null) return Array.Empty<string>();
            var cities = intent.Cities.Length > 0
                ? intent.Cities
                : (string.IsNullOrWhiteSpace(intent.City) ? Array.Empty<string>() : new[] { intent.City });

            return cities
                .SelectMany(CityCoordinates.GetEquivalentNames)
                .Where(city => !string.IsNullOrWhiteSpace(city))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static IReadOnlyList<EventGenre> GetGenreFilters(AiSearchIntent? intent)
        {
            if (intent == null) return Array.Empty<EventGenre>();
            var genres = intent.Genres.Length > 0
                ? intent.Genres
                : (intent.Genre.HasValue ? new[] { intent.Genre.Value } : Array.Empty<EventGenre>());

            return genres.Distinct().ToArray();
        }

        private static IReadOnlyList<string> BuildKeywordTerms(string? keyword, AiSearchIntent? intent)
        {
            var terms = new List<string>();
            if (!string.IsNullOrWhiteSpace(keyword)) terms.Add(keyword.Trim());
            if (intent?.Keywords is { Length: > 0 })
            {
                terms.AddRange(intent.Keywords.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim()));
            }

            return terms
                .SelectMany(term => term.Split(new[] { ' ', ',', ';', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Where(term => term.Length >= 2)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToArray();
        }

        private static IQueryable<Event> ApplyGenreSearch(IQueryable<Event> query, IReadOnlyList<EventGenre> genres)
        {
            var enumMatches = query.Where(e => genres.Contains(e.Genre));
            var tagMatches = query.Where(_ => false);

            foreach (var genre in genres)
            {
                var token = EventGenreTags.Token(genre);
                tagMatches = tagMatches.Concat(query.Where(e => e.GenreTags != null && e.GenreTags.Contains(token)));
            }

            return enumMatches.Concat(tagMatches).Distinct();
        }

        private static IQueryable<Event> ApplyKeywordSearch(IQueryable<Event> query, IReadOnlyList<string> terms)
        {
            var filtered = query.Where(_ => false);
            foreach (var term in terms)
            {
                var pattern = "%" + EscapeLike(term) + "%";
                filtered = filtered.Concat(query.Where(e =>
                    EF.Functions.ILike(e.Title, pattern) ||
                    (e.Description != null && EF.Functions.ILike(e.Description, pattern)) ||
                    EF.Functions.ILike(e.Address, pattern) ||
                    EF.Functions.ILike(e.City, pattern)));
            }

            return filtered.Distinct();
        }

        private static string EscapeLike(string value)
        {
            return value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        }

        private static IReadOnlyList<EventMapMarkerViewModel> BuildMarkers(IReadOnlyList<EventCardViewModel> events)
        {
            var byCity = events
                .Where(e => !e.Latitude.HasValue || !e.Longitude.HasValue)
                .GroupBy(e => e.City, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Id).ToList(), StringComparer.OrdinalIgnoreCase);

            var result = new List<EventMapMarkerViewModel>(events.Count);

            foreach (var ev in events)
            {
                double lat, lng;
                bool approx = false;

                if (ev.Latitude.HasValue && ev.Longitude.HasValue)
                {
                    lat = ev.Latitude.Value;
                    lng = ev.Longitude.Value;
                }
                else if (CityCoordinates.TryGetCoordinates(ev.City, out var clat, out var clng))
                {
                    var idx = byCity.TryGetValue(ev.City, out var ids) ? ids.IndexOf(ev.Id) : 0;
                    var (offLat, offLng) = JitterOffset(idx);
                    lat = clat + offLat;
                    lng = clng + offLng;
                    approx = true;
                }
                else
                {
                    continue;
                }

                result.Add(new EventMapMarkerViewModel
                {
                    EventId = ev.Id,
                    Title = ev.Title,
                    City = ev.City,
                    Address = ev.Address,
                    StartTime = ev.StartTime,
                    Genre = ev.Genre,
                    ImageUrl = ev.ImageUrl,
                    OrganizerName = ev.OrganizerName,
                    Lat = lat,
                    Lng = lng,
                    IsApproximate = approx,
                });
            }

            return result;
        }

        private static (double dLat, double dLng) JitterOffset(int index)
        {
            if (index <= 0) return (0, 0);
            const double step = 0.0035;
            var ring = (int)Math.Ceiling(Math.Sqrt(index));
            var angle = (index * 137.508) * Math.PI / 180.0;
            return (Math.Sin(angle) * step * ring, Math.Cos(angle) * step * ring);
        }
    }
}
