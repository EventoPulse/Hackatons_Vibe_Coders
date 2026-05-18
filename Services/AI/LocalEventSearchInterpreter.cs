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

        // Aliases include the canonical Bulgarian + English spellings
        // plus colloquial nicknames Bulgarians use in everyday speech.
        // Nicknames are deliberate: a user typing "малката Виена" must
        // hit Plovdiv without depending on the AI being online or the
        // OpenAI model knowing the reference.
        private static readonly Dictionary<string, string[]> CityAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Sofia"] = new[] { "софия", "sofia", "sofiaq", "столицата" },
            ["Plovdiv"] = new[] { "пловдив", "plovdiv",
                // "Града под тепетата" / "тепетата" / "града на седемте
                // хълма" — standard Plovdiv nicknames. NOTE: "малката
                // Виена" historically refers to Ruse (Austro-Hungarian
                // architecture along the Danube), not Plovdiv — see the
                // Ruse aliases below.
                "града под тепетата", "градът под тепетата", "тепетата",
                "града на седемте хълма", "градът на седемте хълма" },
            ["Varna"] = new[] { "варна", "varna", "морската столица" },
            ["Burgas"] = new[] { "бургас", "burgas", "bourgas" },
            ["Ruse"] = new[] { "русе", "ruse", "rousse",
                // Ruse is the canonical "Малката Виена" — the 19th-century
                // Habsburg-era architecture along the Danube earned it
                // that nickname long before any other Bulgarian city.
                "малката виена", "крайдунавска столица" },
            ["Botevgrad"] = new[] { "ботевград", "botevgrad",
                // Mount Murgash near Botevgrad hosts the tallest TV
                // tower in Bulgaria (211 m), so a query about that
                // landmark must resolve here, not Sofia. We anchor on
                // the *distinctive* phrase ("най-голямата/-високата
                // телевизионна кула") rather than the full sentence,
                // so phrasings like "града където се намира …" also
                // match.
                "най-голямата телевизионна кула",
                "най-високата телевизионна кула",
                "тв кулата на мургаш",
                "телевизионната кула на мургаш",
                "мургаш" },
            ["Stara Zagora"] = new[] { "стара загора", "stara zagora", "града на липите" },
            ["Pleven"] = new[] { "плевен", "pleven" },
            ["Sliven"] = new[] { "сливен", "sliven", "града на стоте войводи" },
            ["Dobrich"] = new[] { "добрич", "dobrich" },
            ["Shumen"] = new[] { "шумен", "shumen" },
            ["Pernik"] = new[] { "перник", "pernik" },
            ["Haskovo"] = new[] { "хасково", "haskovo" },
            ["Yambol"] = new[] { "ямбол", "yambol" },
            ["Pazardzhik"] = new[] { "пазарджик", "pazardzhik" },
            ["Blagoevgrad"] = new[] { "благоевград", "blagoevgrad" },
            ["Veliko Tarnovo"] = new[] { "велико търново", "veliko tarnovo", "tarnovo", "старата столица" },
            ["Vratsa"] = new[] { "враца", "vratsa" },
            ["Gabrovo"] = new[] { "габрово", "gabrovo", "столицата на хумора" },
            ["Asenovgrad"] = new[] { "асеновград", "asenovgrad" },
            ["Vidin"] = new[] { "видин", "vidin" },
            ["Kazanlak"] = new[] { "казанлък", "kazanlak", "розовата долина", "долината на розите" },
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

        // Radius keywords. "околието" / "наблизо" / "около мен" / "около София" / "около Русе"
        // all map to a 30 km bounding box around the city (or the user's GPS).
        private const int DefaultRadiusKm = 30;
        private static readonly string[] RadiusTerms =
        {
            "околието","околие","околността","околностите","наблизо","в близост","в района","в района на",
            "nearby","around"
        };
        // Detect "от HH:MM до HH:MM" / "between 13 and 18" / "13:00-18:00".
        private static readonly Regex TimeRangeRegex = new(
            @"(?:от\s+|between\s+|from\s+)?(?<a>\d{1,2})(?::(?<aMin>\d{2}))?\s*(?:[-–—]|до|to|until)\s*(?<b>\d{1,2})(?::(?<bMin>\d{2}))?(?:\s*ч(?:аса)?|\s*h(?:ours?)?)?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
            var (startTime, endTime) = DetectTimeRange(raw, consumed);

            // "около мен" / "near me" — always a radius search anchored on
            // the user's GPS. We don't set Lat/Lng here (the client does
            // when it has the coordinates) but flag NearMe + RadiusKm.
            var nearMe = ContainsAny(normalized, new[] { "около мен", "близо до мен", "near me", "nearby", "around me" });
            if (nearMe)
            {
                MarkConsumed(consumed, "около");
                MarkConsumed(consumed, "мен");
                MarkConsumed(consumed, "near");
                MarkConsumed(consumed, "me");
            }

            // "околието" / "околността" — radius around the named city
            // (or around the user if no city given).
            var wantsRadius = ContainsAny(normalized, RadiusTerms);
            if (wantsRadius)
            {
                foreach (var term in RadiusTerms) MarkConsumed(consumed, term);
            }

            int? radiusKm = null;
            if (wantsRadius || nearMe) radiusKm = DefaultRadiusKm;

            var keywords = tokens
                .Where(t => t.Length >= 2)
                .Where(t => !FillerWords.Contains(t, StringComparer.OrdinalIgnoreCase))
                .Where(t => !consumed.Contains(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(6)
                .ToArray();

            var hasStructured = cities.Count > 0 || genres.Count > 0 || dateFrom.HasValue || dateTo.HasValue
                || nearMe || wantsRadius || startTime.HasValue || endTime.HasValue;
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
                RadiusKm = radiusKm,
                StartTimeOfDay = startTime,
                EndTimeOfDay = endTime,
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
            // Always allow the AI follow-up for the new long-query format
            // ("Утре ще съм в София от 13:00 до 18:00 може ли да ми
            // препоръчаш събития ..."). The 180-char ceiling lived in the
            // old controller and is no longer relevant.
            var shouldAskAi = !accountedFor && wordCount >= 4;

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
                RadiusKm = local.RadiusKm ?? ai.RadiusKm,
                StartTimeOfDay = local.StartTimeOfDay ?? ai.StartTimeOfDay,
                EndTimeOfDay = local.EndTimeOfDay ?? ai.EndTimeOfDay,
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

            // Order matters — "вдругиден" / "day after tomorrow" must be
            // checked before plain "утре" / "tomorrow" so a query like
            // "вдругиден в София" doesn't get matched as plain tomorrow.
            if (ContainsAny(normalized, new[] { "вдругиден", "в други ден", "други ден", "day after tomorrow" }))
            {
                MarkConsumed(consumed, "вдругиден");
                MarkConsumed(consumed, "други");
                MarkConsumed(consumed, "ден");
                MarkConsumed(consumed, "day");
                MarkConsumed(consumed, "after");
                MarkConsumed(consumed, "tomorrow");
                return (today.AddDays(2), today.AddDays(2), "day after tomorrow");
            }

            if (ContainsAny(normalized, new[] { "утре", "tomorrow" }))
            {
                MarkConsumed(consumed, "утре");
                MarkConsumed(consumed, "tomorrow");
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

        // Parses time ranges like "от 13:00 до 18:00", "13-18", "13ч до 18",
        // "between 13 and 18". Returns (start, end) in 24h Bulgarian local
        // time. The matched substring is *not* added to `consumed` because
        // it's not a word — the keywords step already filters out short
        // numeric tokens.
        private static (TimeSpan? Start, TimeSpan? End) DetectTimeRange(string raw, HashSet<string> consumed)
        {
            var match = TimeRangeRegex.Match(raw);
            if (!match.Success) return (null, null);

            if (!int.TryParse(match.Groups["a"].Value, out var a)) return (null, null);
            if (!int.TryParse(match.Groups["b"].Value, out var b)) return (null, null);
            if (a < 0 || a > 24 || b < 0 || b > 24) return (null, null);

            var aMin = 0; var bMin = 0;
            if (match.Groups["aMin"].Success) int.TryParse(match.Groups["aMin"].Value, out aMin);
            if (match.Groups["bMin"].Success) int.TryParse(match.Groups["bMin"].Value, out bMin);
            if (aMin < 0 || aMin > 59 || bMin < 0 || bMin > 59) return (null, null);

            var start = TimeSpan.FromMinutes(a * 60 + aMin);
            var end = TimeSpan.FromMinutes(b * 60 + bMin);
            // Sanity: a "13 до 18" with end <= start is suspicious — drop.
            if (end <= start) return (null, null);

            MarkConsumed(consumed, "от"); MarkConsumed(consumed, "до");
            MarkConsumed(consumed, "between"); MarkConsumed(consumed, "and");
            MarkConsumed(consumed, "from"); MarkConsumed(consumed, "to");
            MarkConsumed(consumed, "until");
            MarkConsumed(consumed, "часа"); MarkConsumed(consumed, "час");
            MarkConsumed(consumed, "hours"); MarkConsumed(consumed, "hour");
            return (start, end);
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
