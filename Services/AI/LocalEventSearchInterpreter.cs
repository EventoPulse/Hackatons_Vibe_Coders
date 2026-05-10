using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using EventsApp.Common;
using EventsApp.Models;

namespace EventsApp.Services.AI
{
    public sealed class LocalSearchParse
    {
        public AiSearchIntent? Intent { get; init; }
        public bool HasStrongIntent { get; init; }
        public bool ShouldAskAi { get; init; }
    }

    public static class LocalEventSearchInterpreter
    {
        private static readonly Regex TokenRegex = new(@"[\p{L}\p{Nd}]+", RegexOptions.Compiled);

        private static readonly string[] FillerWords =
        {
            "искам", "иска", "търся", "търси", "намери", "покажи", "дай", "има", "ли",
            "събитие", "събития", "event", "events", "please", "pls", "want", "looking", "show", "find",
            "за", "на", "в", "около", "близо", "до", "near", "me", "with", "the", "a", "an", "and", "or"
        };

        private static readonly Dictionary<string, string[]> CityAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Sofia"] = new[] { "софия", "sofia", "sofiaq" },
            ["Plovdiv"] = new[] { "пловдив", "plovdiv" },
            ["Varna"] = new[] { "варна", "varna" },
            ["Burgas"] = new[] { "бургас", "burgas", "bourgas" },
            ["Ruse"] = new[] { "русе", "ruse", "rousse" },
            ["Stara Zagora"] = new[] { "стара загора", "stara zagora" },
            ["Pleven"] = new[] { "плевен", "pleven" },
            ["Sliven"] = new[] { "сливен", "sliven" },
            ["Dobrich"] = new[] { "добрич", "dobrich" },
            ["Shumen"] = new[] { "шумен", "shumen" },
            ["Pernik"] = new[] { "перник", "pernik" },
            ["Haskovo"] = new[] { "хасково", "haskovo" },
            ["Yambol"] = new[] { "ямбол", "yambol" },
            ["Pazardzhik"] = new[] { "пазарджик", "pazardzhik" },
            ["Blagoevgrad"] = new[] { "благоевград", "blagoevgrad" },
            ["Veliko Tarnovo"] = new[] { "велико търново", "veliko tarnovo", "tarnovo" },
            ["Vratsa"] = new[] { "враца", "vratsa" },
            ["Gabrovo"] = new[] { "габрово", "gabrovo" },
            ["Asenovgrad"] = new[] { "асеновград", "asenovgrad" },
            ["Vidin"] = new[] { "видин", "vidin" },
            ["Kazanlak"] = new[] { "казанлък", "kazanlak" },
            ["Kyustendil"] = new[] { "кюстендил", "kyustendil" },
            ["Montana"] = new[] { "монтана", "montana" },
            ["Targovishte"] = new[] { "търговище", "targovishte" },
            ["Razgrad"] = new[] { "разград", "razgrad" },
            ["Silistra"] = new[] { "силистра", "silistra" },
        };

        private static readonly (string[] Terms, EventGenre[] Genres)[] GenreAliases =
        {
            (new[] { "парти", "партита", "party", "parties", "клуб", "club", "nightlife", "дискотека", "бар", "dance" },
                new[] { EventGenre.Nightlife, EventGenre.Electronic, EventGenre.House, EventGenre.Techno, EventGenre.Pop, EventGenre.Chalga }),
            (new[] { "море", "морето", "плаж", "плажа", "черноморие", "черно море", "sea", "beach", "coast", "seaside" },
                new[] { EventGenre.Nightlife, EventGenre.Festival, EventGenre.LiveMusic, EventGenre.Electronic, EventGenre.House, EventGenre.Pop }),
            (new[] { "техно", "techno", "rave", "рейв" }, new[] { EventGenre.Techno, EventGenre.Electronic }),
            (new[] { "хаус", "house" }, new[] { EventGenre.House, EventGenre.Electronic }),
            (new[] { "електрон", "electronic", "edm" }, new[] { EventGenre.Electronic, EventGenre.Techno, EventGenre.House }),
            (new[] { "джаз", "jazz" }, new[] { EventGenre.Jazz }),
            (new[] { "рок", "rock" }, new[] { EventGenre.Rock }),
            (new[] { "метъл", "metal" }, new[] { EventGenre.Metal }),
            (new[] { "поп", "pop" }, new[] { EventGenre.Pop }),
            (new[] { "хип хоп", "хип-хоп", "hip hop", "hip-hop", "rap", "рап" }, new[] { EventGenre.HipHop, EventGenre.Trap, EventGenre.Rnb }),
            (new[] { "чалга", "chalga", "folk" }, new[] { EventGenre.Chalga, EventGenre.Folk }),
            (new[] { "концерт", "концерти", "live", "жива музика", "музика" }, new[] { EventGenre.LiveMusic, EventGenre.Pop, EventGenre.Rock, EventGenre.Jazz, EventGenre.Folk }),
            (new[] { "театър", "театърът", "theater", "theatre", "сцена" }, new[] { EventGenre.Theater }),
            (new[] { "комедия", "standup", "stand up", "стендъп", "стендъп" }, new[] { EventGenre.Comedy, EventGenre.Standup }),
            (new[] { "опера", "opera" }, new[] { EventGenre.Opera, EventGenre.Classical }),
            (new[] { "балет", "ballet" }, new[] { EventGenre.Ballet, EventGenre.Classical }),
            (new[] { "класика", "класическа", "classical", "classic" }, new[] { EventGenre.Classical }),
            (new[] { "кино", "cinema", "movie", "film", "филм" }, new[] { EventGenre.Cinema }),
            (new[] { "деца", "детски", "семейно", "kids", "children", "family" }, new[] { EventGenre.Kids }),
            (new[] { "фестивал", "festival", "fest" }, new[] { EventGenre.Festival }),
            (new[] { "изложба", "галерия", "art", "gallery", "exhibition" }, new[] { EventGenre.Exhibition, EventGenre.Art }),
            (new[] { "спорт", "sports", "мач", "game" }, new[] { EventGenre.Sports }),
            (new[] { "храна", "напитки", "food", "drinks", "wine", "вино", "бира" }, new[] { EventGenre.FoodAndDrinks }),
            (new[] { "работилница", "workshop", "курс", "лекция" }, new[] { EventGenre.Workshop, EventGenre.Conference }),
            (new[] { "networking", "нетуъркинг", "бизнес" }, new[] { EventGenre.Networking, EventGenre.Conference }),
            (new[] { "игри", "gaming", "game night" }, new[] { EventGenre.Gaming }),
        };

        private static readonly string[] SeaTerms =
        {
            "море", "морето", "плаж", "плажа", "черноморие", "черно море", "sea", "beach", "coast", "seaside"
        };

        public static LocalSearchParse Parse(string? query, DateTime now)
        {
            var raw = (query ?? string.Empty).Trim();
            if (raw.Length == 0)
            {
                return new LocalSearchParse();
            }

            var normalized = Normalize(raw);
            var tokens = Tokenize(normalized);
            var consumed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var cities = DetectCities(normalized, consumed);
            if (ContainsAny(normalized, SeaTerms))
            {
                AddDistinct(cities, "Varna");
                AddDistinct(cities, "Burgas");
                AddDistinct(cities, "Dobrich");
                foreach (var term in SeaTerms) MarkConsumed(consumed, term);
            }

            var genres = DetectGenres(normalized, consumed);
            var (dateFrom, dateTo, dateIntent) = DetectDate(normalized, now, consumed);
            var nearMe = ContainsAny(normalized, new[] { "около мен", "близо до мен", "near me", "nearby", "around me" });
            if (nearMe)
            {
                MarkConsumed(consumed, "около");
                MarkConsumed(consumed, "мен");
                MarkConsumed(consumed, "near");
                MarkConsumed(consumed, "me");
            }

            var keywords = tokens
                .Where(t => t.Length >= 2)
                .Where(t => !FillerWords.Contains(t, StringComparer.OrdinalIgnoreCase))
                .Where(t => !consumed.Contains(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(6)
                .ToArray();

            var hasStructured = cities.Count > 0 || genres.Count > 0 || dateFrom.HasValue || dateTo.HasValue || nearMe;
            var useKeyword = !hasStructured || keywords.Length > 0 && genres.Count == 0;

            var intent = new AiSearchIntent
            {
                RawQuery = raw,
                City = cities.FirstOrDefault(),
                Cities = cities.ToArray(),
                Genre = genres.Count > 0 ? genres[0] : null,
                Genres = genres.ToArray(),
                DateFrom = dateFrom,
                DateTo = dateTo,
                DateIntent = dateIntent,
                NearMe = nearMe,
                Keyword = useKeyword ? string.Join(' ', keywords) : null,
                Keywords = useKeyword ? keywords : Array.Empty<string>(),
            };

            if (intent.City != null && CityCoordinates.TryGetCoordinates(intent.City, out var lat, out var lng))
            {
                intent.Latitude = lat;
                intent.Longitude = lng;
            }

            var accountedFor = hasStructured && keywords.Length <= 1;
            var wordCount = tokens.Count;
            var shouldAskAi = !accountedFor && wordCount >= 4 && raw.Length <= 180;

            if (!hasStructured && keywords.Length == 0)
            {
                return new LocalSearchParse { ShouldAskAi = shouldAskAi };
            }

            return new LocalSearchParse
            {
                Intent = intent,
                HasStrongIntent = accountedFor,
                ShouldAskAi = shouldAskAi,
            };
        }

        public static AiSearchIntent Merge(AiSearchIntent? local, AiSearchIntent? ai)
        {
            if (local == null) return ai ?? new AiSearchIntent();
            if (ai == null) return local;

            var cities = local.Cities.Concat(ai.Cities).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var genres = local.Genres.Concat(ai.Genres).Distinct().ToArray();
            if (genres.Length == 0 && ai.Genre.HasValue) genres = new[] { ai.Genre.Value };
            if (genres.Length == 0 && local.Genre.HasValue) genres = new[] { local.Genre.Value };

            return new AiSearchIntent
            {
                RawQuery = local.RawQuery ?? ai.RawQuery,
                City = local.City ?? ai.City ?? cities.FirstOrDefault(),
                Cities = cities.Length > 0
                    ? cities
                    : new[] { local.City, ai.City }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                Genre = local.Genre ?? ai.Genre ?? (genres.Length > 0 ? genres[0] : null),
                Genres = genres,
                DateFrom = local.DateFrom ?? ai.DateFrom,
                DateTo = local.DateTo ?? ai.DateTo,
                DateIntent = local.DateIntent ?? ai.DateIntent,
                NearMe = local.NearMe || ai.NearMe,
                Latitude = local.Latitude ?? ai.Latitude,
                Longitude = local.Longitude ?? ai.Longitude,
                Keyword = !string.IsNullOrWhiteSpace(local.Keyword) ? local.Keyword : ai.Keyword,
                Keywords = local.Keywords.Length > 0 ? local.Keywords : ai.Keywords,
                Explanation = ai.Explanation ?? local.Explanation,
            };
        }

        private static List<string> DetectCities(string normalized, HashSet<string> consumed)
        {
            var cities = new List<string>();
            foreach (var (canonical, aliases) in CityAliases)
            {
                foreach (var alias in aliases)
                {
                    if (!ContainsTerm(normalized, alias)) continue;
                    AddDistinct(cities, canonical);
                    MarkConsumed(consumed, alias);
                    break;
                }
            }
            return cities;
        }

        private static List<EventGenre> DetectGenres(string normalized, HashSet<string> consumed)
        {
            var genres = new List<EventGenre>();
            foreach (var (terms, values) in GenreAliases)
            {
                if (!ContainsAny(normalized, terms)) continue;
                foreach (var genre in values) AddDistinct(genres, genre);
                foreach (var term in terms) MarkConsumed(consumed, term);
            }
            return genres;
        }

        private static (DateTime? From, DateTime? To, string? Intent) DetectDate(string normalized, DateTime now, HashSet<string> consumed)
        {
            var today = now.Date;
            if (ContainsAny(normalized, new[] { "днес", "тази вечер", "вечерта", "today", "tonight" }))
            {
                MarkConsumed(consumed, "днес");
                MarkConsumed(consumed, "тази");
                MarkConsumed(consumed, "вечер");
                return (today, today, "today");
            }

            if (ContainsAny(normalized, new[] { "утре", "tomorrow" }))
            {
                MarkConsumed(consumed, "утре");
                return (today.AddDays(1), today.AddDays(1), "tomorrow");
            }

            if (ContainsAny(normalized, new[] { "уикенд", "weekend", "събота", "неделя" }))
            {
                var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)today.DayOfWeek + 7) % 7;
                if (daysUntilSaturday == 0 && now.DayOfWeek == DayOfWeek.Sunday) daysUntilSaturday = 6;
                var saturday = today.AddDays(daysUntilSaturday);
                MarkConsumed(consumed, "уикенд");
                MarkConsumed(consumed, "weekend");
                return (saturday, saturday.AddDays(1), "this weekend");
            }

            if (ContainsAny(normalized, new[] { "тази седмица", "седмицата", "this week" }))
            {
                var daysUntilSunday = (7 - (int)today.DayOfWeek) % 7;
                MarkConsumed(consumed, "седмица");
                return (today, today.AddDays(daysUntilSunday), "this week");
            }

            return (null, null, null);
        }

        private static string Normalize(string value)
        {
            var lower = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(lower.Length);
            foreach (var ch in lower)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category == UnicodeCategory.NonSpacingMark) continue;
                sb.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
            }
            return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        }

        private static List<string> Tokenize(string normalized) =>
            TokenRegex.Matches(normalized).Select(m => m.Value).ToList();

        private static bool ContainsAny(string normalized, IEnumerable<string> terms) =>
            terms.Any(term => ContainsTerm(normalized, Normalize(term)));

        private static bool ContainsTerm(string normalized, string term)
        {
            term = Normalize(term);
            if (term.Length == 0) return false;
            return (" " + normalized + " ").Contains(" " + term + " ", StringComparison.OrdinalIgnoreCase);
        }

        private static void MarkConsumed(HashSet<string> consumed, string phrase)
        {
            foreach (var token in Tokenize(Normalize(phrase)))
            {
                consumed.Add(token);
            }
        }

        private static void AddDistinct<T>(List<T> list, T value)
        {
            if (!list.Contains(value)) list.Add(value);
        }
    }
}
