using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EventsApp.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace EventsApp.Services.AI
{
    public class OpenAiService : IAiSearchService
    {
        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private static readonly string SearchSystemPrompt =
@"You are a STRUCTURED filter parser for Evento, an events platform in Bulgaria.
Your only job is to extract filters from the user's natural-language query.
Return ONLY valid JSON. No markdown, no explanations, no extra text.
You MUST NOT invent events, venues, artists, prices, or links.
If you are unsure about any field, set it to null. Do not guess.

JSON schema (every key must appear; use null / empty array if missing):
{
  ""city"":      string|null,
  ""cities"":    string[],
  ""genre"":     string|null,
  ""genres"":    string[],
  ""keyword"":   string|null,
  ""keywords"":  string[],
  ""dateIntent"": string|null,
  ""dateFrom"":  string|null,           // ISO date YYYY-MM-DD in Europe/Sofia local time
  ""dateTo"":    string|null,           // ISO date YYYY-MM-DD in Europe/Sofia local time
  ""startTimeOfDay"": string|null,      // HH:mm 24h, Europe/Sofia local time
  ""endTimeOfDay"":   string|null,      // HH:mm 24h, Europe/Sofia local time
  ""nearMe"":    boolean,                // true ONLY if user mentions ""около мен"" / ""near me""
  ""radiusKm"":  number|null,            // 30 by default when the user uses ""околието"", ""наблизо"", ""около <град>""
  ""latitude"":  number|null,
  ""longitude"": number|null
}

Cities (always return canonical English spelling, or null if not present):
София → Sofia, Пловдив → Plovdiv, Варна → Varna, Бургас → Burgas, Русе → Ruse,
Стара Загора → Stara Zagora, Плевен → Pleven, Велико Търново → Veliko Tarnovo,
Благоевград → Blagoevgrad, Шумен → Shumen, Добрич → Dobrich, Сливен → Sliven,
Перник → Pernik, Хасково → Haskovo, Ямбол → Yambol, Пазарджик → Pazardzhik,
Враца → Vratsa, Габрово → Gabrovo, Асеновград → Asenovgrad, Видин → Vidin,
Казанлък → Kazanlak, Кюстендил → Kyustendil, Монтана → Montana, Търговище → Targovishte,
Разград → Razgrad, Силистра → Silistra.

Bulgarian colloquial city nicknames (resolve to the canonical city):
""столицата""                                 → Sofia
""малката Виена"" / ""градът под тепетата"" / ""тепетата""  → Plovdiv
""морската столица""                           → Varna
""старата столица""                            → Veliko Tarnovo
""розовата долина"" / ""долината на розите""    → Kazanlak
""столицата на хумора""                        → Gabrovo
""крайдунавска столица""                       → Ruse
""града на липите""                            → Stara Zagora
""града на стоте войводи""                     → Sliven

Genre enum names (use these exactly):
Nightlife, Electronic, House, Techno, Pop, Rock, Metal, Jazz, HipHop, Trap, Rnb,
Folk, Chalga, LiveMusic, Classical, Opera, Ballet, Theater, Comedy, Standup,
Cinema, Kids, Festival, Exhibition, Art, Sports, FoodAndDrinks, Workshop,
Networking, Gaming, Conference, Other.

Hard rules:
- ""около мен"" / ""близо до мен"" / ""near me""           → nearMe=true, radiusKm=30
- ""околието"" / ""околността"" / ""наблизо"" / ""в района"" → radiusKm=30; cities stays the named city
- ""в София и околието""  → city=""Sofia"", cities=[""Sofia""], radiusKm=30
- ""в Русе и околието""   → city=""Ruse"",  cities=[""Ruse""],  radiusKm=30
- Sea/beach/coast/морето/плаж/Черноморие → cities=[""Varna"",""Burgas"",""Dobrich""]
- ""утре"" → dateFrom=dateTo=tomorrow in Sofia. ""днес"" → today. ""вдругиден"" → +2 days.
  ""уикенд"" → Saturday..Sunday. ""тази седмица"" → today..end-of-week.
- Time ranges (""от 13:00 до 18:00"", ""13-18ч"", ""between 1pm and 6pm"") →
  startTimeOfDay=""13:00"", endTimeOfDay=""18:00"". 24h format.
- ignore filler words: искам, искам да, търся, ще съм, може ли, препоръчаш,
  събитие, събития, event, events, please, show, find.
- keyword: optional short free-text only when the user describes a vibe that
  no city / genre / date / time captures. Otherwise null.
- If absolutely nothing structured can be extracted, return all-null/empty fields.
  Never echo the user query back as keyword.

Examples:

Examples assume today in Europe/Sofia is provided in the user message. Use that
to resolve relative dates — do not assume any other date.

User query: ""Събития в Русе и околието""
Output: {""city"":""Ruse"",""cities"":[""Ruse""],""genre"":null,""genres"":[],""keyword"":null,""keywords"":[],""dateIntent"":null,""dateFrom"":null,""dateTo"":null,""startTimeOfDay"":null,""endTimeOfDay"":null,""nearMe"":false,""radiusKm"":30,""latitude"":null,""longitude"":null}

User query (with ""Today (Europe/Sofia): 2026-05-18""): ""Утре ще съм в София от 13:00 до 18:00 може ли да ми препоръчаш събития""
Output: {""city"":""Sofia"",""cities"":[""Sofia""],""genre"":null,""genres"":[],""keyword"":null,""keywords"":[],""dateIntent"":""tomorrow"",""dateFrom"":""2026-05-19"",""dateTo"":""2026-05-19"",""startTimeOfDay"":""13:00"",""endTimeOfDay"":""18:00"",""nearMe"":false,""radiusKm"":null,""latitude"":null,""longitude"":null}

User query (with ""Today (Europe/Sofia): 2026-05-18, Sunday""): ""джаз концерт тази седмица в Пловдив""
Output: {""city"":""Plovdiv"",""cities"":[""Plovdiv""],""genre"":""Jazz"",""genres"":[""Jazz"",""LiveMusic""],""keyword"":null,""keywords"":[],""dateIntent"":""this week"",""dateFrom"":""2026-05-18"",""dateTo"":""2026-05-24"",""startTimeOfDay"":null,""endTimeOfDay"":null,""nearMe"":false,""radiusKm"":null,""latitude"":null,""longitude"":null}

Return ONLY the JSON object.";

        private static string GetDescriptionSystemPrompt(string? lang) => (lang == "en")
            ? "You are a marketing copywriter for an events platform. Write a single concise event description (60-120 words) in plain English. Tone: energetic, inviting, modern. Do NOT invent artist names, prices, or times. Output ONLY the description text."
            : "Ти си маркетинг копирайтър за платформа за събития. Напиши кратко описание на събитието (60-120 думи) на естествен, жив български. Тон: енергичен, привлекателен, модерен. НЕ измисляй имена на артисти, цени или часове. Изведи САМО текста на описанието.";

        private readonly HttpClient _http;
        private readonly AiOptions _opts;
        private readonly ILogger<OpenAiService> _logger;
        private readonly IMemoryCache _cache;

        public AiStatus LastStatus { get; private set; } = AiStatus.Disabled;
        public string? LastStatusDetail { get; private set; }

        public OpenAiService(HttpClient http, IOptions<AiOptions> opts, ILogger<OpenAiService> logger, IMemoryCache cache)
        {
            _http = http;
            _opts = opts.Value;
            _logger = logger;
            _cache = cache;

            if (_opts.IsConfigured)
            {
                _http.BaseAddress = new Uri("https://api.openai.com/v1/");
                _http.DefaultRequestHeaders.Remove("Authorization");
                _http.DefaultRequestHeaders.Add("Authorization", "Bearer " + _opts.ApiKey);
                _http.Timeout = TimeSpan.FromSeconds(Math.Max(5, _opts.TimeoutSeconds));
                LastStatus = AiStatus.Ok;
            }
            else
            {
                LastStatus = AiStatus.Disabled;
                LastStatusDetail = "AI:ApiKey is empty.";
            }
        }

        public bool IsEnabled => _opts.IsConfigured;

        public Task<AiSearchIntent?> InterpretAsync(string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query)) return Task.FromResult<AiSearchIntent?>(null);
            var trimmed = query.Trim();
            if (trimmed.Length > _opts.MaxSearchQueryLength)
            {
                trimmed = trimmed[.._opts.MaxSearchQueryLength];
            }

            var cacheKey = "ai-search:" + Sha256(trimmed.ToLowerInvariant());
            if (_cache.TryGetValue(cacheKey, out AiSearchIntent? cached))
            {
                LastStatus = AiStatus.Ok;
                LastStatusDetail = "Cached";
                return Task.FromResult(cached);
            }

            return InterpretCachedAsync(trimmed, cacheKey, cancellationToken);
        }

        private async Task<AiSearchIntent?> InterpretCachedAsync(string query, string cacheKey, CancellationToken cancellationToken)
        {
            var intent = await InterpretInternalAsync(query, cancellationToken);
            if (intent != null)
            {
                _cache.Set(cacheKey, intent, TimeSpan.FromMinutes(Math.Clamp(_opts.SearchCacheMinutes, 5, 24 * 60)));
            }

            return intent;
        }

        private async Task<AiSearchIntent?> InterpretInternalAsync(string query, CancellationToken cancellationToken)
        {
            var sofia = TryGetSofiaTimeZone();
            var nowSofia = sofia == null
                ? DateTime.UtcNow
                : TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, sofia);
            var todayLine = "Today (Europe/Sofia): " + nowSofia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                + ", " + nowSofia.ToString("dddd", CultureInfo.InvariantCulture);
            var userContent = todayLine + "\nUser query: " + query.Trim();

            // 350 tokens leaves enough room for the wider schema (radiusKm,
            // startTimeOfDay, endTimeOfDay) without bloating cost.
            var raw = await CallChatAsync(SearchSystemPrompt, userContent, 350, 0.1, cancellationToken);
            if (raw == null) return null;

            var intent = ParseIntentJson(raw);
            if (intent == null)
            {
                LastStatus = AiStatus.ParseFailed;
                LastStatusDetail = "OpenAI returned a response that didn't match the expected JSON shape.";
                _logger.LogWarning("OpenAI returned unparseable search response: {Body}", Truncate(raw, 300));
                return null;
            }

            intent.RawQuery = query.Trim();
            return intent;
        }

        public async Task<string?> GenerateTextAsync(string prompt, string tag, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return null;
            prompt = Truncate(prompt.Trim(), Math.Clamp(_opts.MaxPromptCharacters, 500, 6000));
            var raw = await CallChatAsync("You are a helpful assistant. Keep answers concise.", prompt, 450, 0.5, cancellationToken);
            if (raw == null) return null;
            var clean = raw.Trim().Trim('"').Trim();
            return string.IsNullOrWhiteSpace(clean) ? null : clean;
        }

        public async Task<DayPlanRequestIntent?> ParseDayPlanRequestAsync(string description, IReadOnlyList<string> knownCities, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(description)) return null;

            var sys = new StringBuilder();
            sys.AppendLine("You are a request parser for an events platform. Extract the day plan request as JSON.");
            sys.AppendLine("Return ONLY JSON. No markdown.");
            sys.AppendLine("Schema: { \"city\": string|null, \"date\": \"YYYY-MM-DD\"|null, \"vibe\": string|null, \"groupContext\": string|null }");
            if (knownCities.Count > 0)
            {
                sys.AppendLine("Known cities (use exact spelling from this list when matching): " + string.Join(", ", knownCities));
            }
            sys.AppendLine("Rules:");
            sys.AppendLine("- Recognize Bulgarian and English city names. Match to a known city if possible.");
            sys.AppendLine("- Resolve relative dates: today, tomorrow, the day after tomorrow, this Friday, etc. Always return ISO date.");
            sys.AppendLine("- vibe: short phrase summarizing mood/genre/energy (e.g. 'live techno', 'chill jazz').");
            sys.AppendLine("- groupContext: who they are with (e.g. 'with friends', 'solo', 'first date'). Null if missing.");

            var user = "Today: " + DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                + "\nUser request: " + description.Trim();

            var raw = await CallChatAsync(sys.ToString(), user, 220, 0.1, cancellationToken);
            if (raw == null) return null;
            return ParseDayPlanIntentJson(raw);
        }

        public async Task<DayPlanTimeline?> GenerateDayPlanTimelineAsync(DayPlanRequestIntent intent, IReadOnlyList<DayPlanEventCandidate> events, CancellationToken cancellationToken = default)
        {
            if (intent == null) return null;

            var sys = new StringBuilder();
            sys.AppendLine("You are a friendly day-plan composer for the Evento events platform in Bulgaria.");
            sys.AppendLine("Build a short timeline using ONLY events from the provided list when picking real events. Do NOT invent events.");
            sys.AppendLine("Output STRICT JSON. No markdown, no commentary.");
            sys.AppendLine("Schema:");
            sys.AppendLine("{");
            sys.AppendLine("  \"title\": string,");
            sys.AppendLine("  \"intro\": string,");
            sys.AppendLine("  \"slots\": [");
            sys.AppendLine("    { \"slot\": \"before\"|\"main\"|\"after\", \"startTime\": \"HH:mm\"|null, \"endTime\": \"HH:mm\"|null, \"title\": string, \"description\": string, \"eventId\": number|null }");
            sys.AppendLine("  ]");
            sys.AppendLine("}");
            sys.AppendLine("Rules:");
            sys.AppendLine("- title: short headline for the day (max 80 chars). In Bulgarian.");
            sys.AppendLine("- intro: 1 sentence in Bulgarian, warm and concrete.");
            sys.AppendLine("- 1 'main' slot is REQUIRED if any events are provided — pick the best fitting one and set eventId to its id.");
            sys.AppendLine("- 0-1 'before' slot (e.g. coffee, dinner, drinks). General description only — no specific venue invented. eventId must be null.");
            sys.AppendLine("- 0-1 'after' slot (e.g. late drinks, walk). General description only. eventId must be null.");
            sys.AppendLine("- Times in 24h HH:mm. Make 'before' end before main starts, 'after' start after main ends.");
            sys.AppendLine("- All text fields in Bulgarian.");
            sys.AppendLine("- If NO events were provided, still return a plan with main slot title 'Няма публикувани събития' and eventId=null.");

            var user = new StringBuilder();
            user.AppendLine($"Град: {intent.City}");
            if (intent.Date.HasValue)
            {
                user.AppendLine($"Дата: {intent.Date.Value:dd.MM.yyyy} ({intent.Date.Value.ToString("dddd", new CultureInfo("bg-BG"))})");
            }
            if (!string.IsNullOrWhiteSpace(intent.Vibe)) user.AppendLine($"Настроение: {intent.Vibe}");
            if (!string.IsNullOrWhiteSpace(intent.GroupContext)) user.AppendLine($"Контекст: {intent.GroupContext}");
            user.AppendLine();
            user.AppendLine("Events available:");
            if (events.Count == 0)
            {
                user.AppendLine("(no events)");
            }
            else
            {
                foreach (var e in events.Take(20))
                {
                    user.AppendLine($"- id={e.Id} | {e.StartTime:HH:mm}-{e.EndTime:HH:mm} | {e.Title} | {e.Genre} | {e.Address}");
                }
            }

            var raw = await CallChatAsync(sys.ToString(), user.ToString(), 600, 0.5, cancellationToken);
            if (raw == null) return null;
            return ParseDayPlanTimelineJson(raw);
        }

        public async Task<string?> GenerateEventDescriptionAsync(string title, string? city, string? genre, string? hints, string? lang = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(title)) return null;

            var sb = new StringBuilder();
            sb.AppendLine("Event title: " + title.Trim());
            if (!string.IsNullOrWhiteSpace(city)) sb.AppendLine("City: " + city.Trim());
            if (!string.IsNullOrWhiteSpace(genre)) sb.AppendLine("Genre: " + genre.Trim());
            if (!string.IsNullOrWhiteSpace(hints)) sb.AppendLine("Extra notes: " + Truncate(hints.Trim(), 500));

            var raw = await CallChatAsync(GetDescriptionSystemPrompt(lang), sb.ToString(), 160, 0.7, cancellationToken);
            if (raw == null) return null;
            var clean = raw.Trim().Trim('"').Trim();
            return string.IsNullOrWhiteSpace(clean) ? null : clean;
        }

        private async Task<string?> CallChatAsync(string system, string user, int maxTokens, double temperature, CancellationToken ct)
        {
            if (!_opts.IsConfigured)
            {
                LastStatus = AiStatus.Disabled;
                return null;
            }

            var payload = new
            {
                model = _opts.ModelName,
                messages = new[]
                {
                    new { role = "system", content = system },
                    new { role = "user",   content = user   },
                },
                max_tokens = maxTokens,
                temperature,
            };

            try
            {
                using var content = new StringContent(JsonSerializer.Serialize(payload, JsonOpts), Encoding.UTF8, "application/json");
                using var resp = await _http.PostAsync("chat/completions", content, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                if (!resp.IsSuccessStatusCode)
                {
                    LastStatus = AiStatus.CallFailed;
                    LastStatusDetail = $"HTTP {(int)resp.StatusCode}: {Truncate(body, 200)}";
                    _logger.LogWarning("OpenAI call failed {Status}: {Body}", (int)resp.StatusCode, Truncate(body, 400));
                    return null;
                }

                using var doc = JsonDocument.Parse(body);
                var text = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                LastStatus = AiStatus.Ok;
                LastStatusDetail = null;
                return text;
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                LastStatus = AiStatus.CallFailed;
                LastStatusDetail = $"OpenAI timed out after {_opts.TimeoutSeconds}s.";
                _logger.LogWarning("OpenAI timed out after {Sec}s", _opts.TimeoutSeconds);
                return null;
            }
            catch (Exception ex)
            {
                LastStatus = AiStatus.CallFailed;
                LastStatusDetail = ex.Message;
                _logger.LogWarning(ex, "OpenAI call failed");
                return null;
            }
        }

        private static AiSearchIntent? ParseIntentJson(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            var s = raw.Trim();
            if (s.StartsWith("```"))
            {
                var first = s.IndexOf('\n');
                var last = s.LastIndexOf("```", StringComparison.Ordinal);
                if (first > 0 && last > first) s = s[(first + 1)..last].Trim();
            }
            var startBrace = s.IndexOf('{');
            var endBrace = s.LastIndexOf('}');
            if (startBrace < 0 || endBrace <= startBrace) return null;
            s = s[startBrace..(endBrace + 1)];

            try
            {
                using var doc = JsonDocument.Parse(s);
                var r = doc.RootElement;
                var intent = new AiSearchIntent
                {
                    City = ReadString(r, "city"),
                    Keyword = ReadString(r, "keyword"),
                    DateIntent = ReadString(r, "dateIntent"),
                    NearMe = r.TryGetProperty("nearMe", out var nm) && nm.ValueKind == JsonValueKind.True,
                    Latitude = ReadDouble(r, "latitude"),
                    Longitude = ReadDouble(r, "longitude"),
                    Keywords = ReadStringArray(r, "keywords"),
                };
                intent.Cities = ReadStringArray(r, "cities");
                if (intent.Cities.Length == 0 && !string.IsNullOrWhiteSpace(intent.City))
                {
                    intent.Cities = new[] { intent.City };
                }

                var genreStr = ReadString(r, "genre");
                if (!string.IsNullOrWhiteSpace(genreStr))
                {
                    if (Enum.TryParse<EventGenre>(genreStr, ignoreCase: true, out var g))
                        intent.Genre = g;
                    else
                    {
                        var normalized = NormalizeGenre(genreStr);
                        if (normalized != null && Enum.TryParse<EventGenre>(normalized, ignoreCase: true, out var g2))
                            intent.Genre = g2;
                    }
                }
                intent.Genres = ReadGenreArray(r, "genres");
                if (intent.Genres.Length == 0 && intent.Genre.HasValue)
                {
                    intent.Genres = new[] { intent.Genre.Value };
                }
                if (!intent.Genre.HasValue && intent.Genres.Length > 0)
                {
                    intent.Genre = intent.Genres[0];
                }

                if (TryParseDate(ReadString(r, "dateFrom"), out var df)) intent.DateFrom = df;
                if (TryParseDate(ReadString(r, "dateTo"), out var dt)) intent.DateTo = dt;

                // New fields — best-effort parse, never throw.
                var radius = ReadDouble(r, "radiusKm");
                if (radius.HasValue && radius.Value > 0 && radius.Value <= 500)
                {
                    intent.RadiusKm = (int)Math.Round(radius.Value);
                }
                if (TryParseTimeOfDay(ReadString(r, "startTimeOfDay"), out var startTod)) intent.StartTimeOfDay = startTod;
                if (TryParseTimeOfDay(ReadString(r, "endTimeOfDay"), out var endTod)) intent.EndTimeOfDay = endTod;
                // Reject malformed windows where end is before start.
                if (intent.StartTimeOfDay.HasValue && intent.EndTimeOfDay.HasValue
                    && intent.EndTimeOfDay.Value <= intent.StartTimeOfDay.Value)
                {
                    intent.StartTimeOfDay = null;
                    intent.EndTimeOfDay = null;
                }

                if (intent.City == null && intent.Keyword == null && intent.Genre == null &&
                    intent.Cities.Length == 0 && intent.Genres.Length == 0 &&
                    intent.DateFrom == null && intent.DateTo == null && !intent.NearMe &&
                    intent.Keywords.Length == 0 && intent.RadiusKm == null &&
                    intent.StartTimeOfDay == null && intent.EndTimeOfDay == null)
                    return null;

                return intent;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static bool TryParseTimeOfDay(string? value, out TimeSpan time)
        {
            time = default;
            if (string.IsNullOrWhiteSpace(value)) return false;
            // Accept "HH:mm", "H:mm", "HH:mm:ss", or bare "HH".
            if (TimeSpan.TryParseExact(value, new[] { @"h\:mm", @"hh\:mm", @"h\:mm\:ss", @"hh\:mm\:ss" }, CultureInfo.InvariantCulture, out time))
                return time.TotalHours >= 0 && time.TotalHours <= 24;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hours)
                && hours >= 0 && hours <= 24)
            {
                time = TimeSpan.FromHours(hours);
                return true;
            }
            return false;
        }

        private static TimeZoneInfo? TryGetSofiaTimeZone()
        {
            foreach (var id in new[] { "Europe/Sofia", "FLE Standard Time" })
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }
            return null;
        }

        private static string? ReadString(JsonElement el, string prop)
        {
            if (!el.TryGetProperty(prop, out var v) || v.ValueKind != JsonValueKind.String) return null;
            var s = v.GetString();
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        }

        private static double? ReadDouble(JsonElement el, string prop)
        {
            if (!el.TryGetProperty(prop, out var v)) return null;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d)) return d;
            return null;
        }

        private static string[] ReadStringArray(JsonElement el, string prop)
        {
            if (!el.TryGetProperty(prop, out var v) || v.ValueKind != JsonValueKind.Array)
                return Array.Empty<string>();
            var list = new List<string>();
            foreach (var item in v.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var s = item.GetString();
                    if (!string.IsNullOrWhiteSpace(s)) list.Add(s.Trim());
                }
            }
            return list.ToArray();
        }

        private static EventGenre[] ReadGenreArray(JsonElement el, string prop)
        {
            if (!el.TryGetProperty(prop, out var v) || v.ValueKind != JsonValueKind.Array)
                return Array.Empty<EventGenre>();

            var list = new List<EventGenre>();
            foreach (var item in v.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String) continue;
                var s = item.GetString();
                if (string.IsNullOrWhiteSpace(s)) continue;
                if (Enum.TryParse<EventGenre>(s, ignoreCase: true, out var genre) && Enum.IsDefined(genre))
                {
                    if (!list.Contains(genre)) list.Add(genre);
                    continue;
                }

                var normalized = NormalizeGenre(s);
                if (normalized != null && Enum.TryParse<EventGenre>(normalized, ignoreCase: true, out var normalizedGenre) && !list.Contains(normalizedGenre))
                {
                    list.Add(normalizedGenre);
                }
            }

            return list.ToArray();
        }

        private static string? NormalizeGenre(string s)
        {
            return s.Trim().ToLowerInvariant() switch
            {
                "техно" or "techno" => "Techno",
                "хаус" or "house" => "House",
                "електронна" or "електронно" or "electronic" or "edm" => "Electronic",
                "хип хоп" or "хип-хоп" or "hip hop" or "hip-hop" or "hiphop" or "rap" or "рап" => "HipHop",
                "поп" or "pop" => "Pop",
                "чалга" or "chalga" => "Chalga",
                "рок" or "rock" => "Rock",
                "джаз" or "jazz" => "Jazz",
                "класика" or "classical" or "classic" => "Classical",
                "парти" or "party" or "клуб" or "club" or "nightlife" => "Nightlife",
                "концерт" or "концерти" or "live" => "LiveMusic",
                "театър" or "theater" or "theatre" => "Theater",
                "фестивал" or "festival" => "Festival",
                _ => null,
            };
        }

        private static bool TryParseDate(string? s, out DateTime result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(s)) return false;
            return DateTime.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out result);
        }

        private static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max] + "...";

        private static string Sha256(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes);
        }

        private static string? ExtractJson(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var s = raw.Trim();
            if (s.StartsWith("```"))
            {
                var first = s.IndexOf('\n');
                var last = s.LastIndexOf("```", StringComparison.Ordinal);
                if (first > 0 && last > first) s = s[(first + 1)..last].Trim();
            }
            var start = s.IndexOf('{');
            var end = s.LastIndexOf('}');
            if (start < 0 || end <= start) return null;
            return s[start..(end + 1)];
        }

        private static DayPlanRequestIntent? ParseDayPlanIntentJson(string raw)
        {
            var json = ExtractJson(raw);
            if (json == null) return null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                var r = doc.RootElement;
                var intent = new DayPlanRequestIntent
                {
                    City = ReadString(r, "city"),
                    Vibe = ReadString(r, "vibe"),
                    GroupContext = ReadString(r, "groupContext"),
                };
                if (TryParseDate(ReadString(r, "date"), out var d)) intent.Date = d.Date;
                return intent;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static DayPlanTimeline? ParseDayPlanTimelineJson(string raw)
        {
            var json = ExtractJson(raw);
            if (json == null) return null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                var r = doc.RootElement;
                var timeline = new DayPlanTimeline
                {
                    Title = ReadString(r, "title"),
                    Intro = ReadString(r, "intro"),
                };

                if (r.TryGetProperty("slots", out var slots) && slots.ValueKind == JsonValueKind.Array)
                {
                    foreach (var s in slots.EnumerateArray())
                    {
                        if (s.ValueKind != JsonValueKind.Object) continue;
                        var slot = new DayPlanTimelineSlot
                        {
                            Slot = ReadString(s, "slot")?.ToLowerInvariant() ?? "main",
                            StartTime = ReadString(s, "startTime"),
                            EndTime = ReadString(s, "endTime"),
                            Title = ReadString(s, "title") ?? "",
                            Description = ReadString(s, "description"),
                        };
                        if (s.TryGetProperty("eventId", out var eid))
                        {
                            if (eid.ValueKind == JsonValueKind.Number && eid.TryGetInt32(out var id)) slot.EventId = id;
                        }
                        if (!string.IsNullOrWhiteSpace(slot.Title))
                            timeline.Slots.Add(slot);
                    }
                }

                return timeline;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
