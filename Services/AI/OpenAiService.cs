using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EventsApp.Models;
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
@"You are a search parser for Evento, an events discovery platform in Bulgaria.
Extract structured search filters from the user query.
Return ONLY valid JSON. No markdown. No explanations.

Schema:
{
  ""city"": string or null,
  ""genre"": string or null,
  ""keyword"": string or null,
  ""dateIntent"": string or null,
  ""dateFrom"": string or null,
  ""dateTo"": string or null,
  ""nearMe"": boolean,
  ""latitude"": number or null,
  ""longitude"": number or null,
  ""keywords"": string[]
}

Rules:
- Detect Bulgarian cities: София/Sofia, Пловдив/Plovdiv, Варна/Varna, Бургас/Burgas, Русе/Ruse, Стара Загора/Stara Zagora, Плевен/Pleven, Велико Търново/Veliko Tarnovo, Благоевград/Blagoevgrad, Шумен/Shumen, Добрич/Dobrich, Сливен/Sliven, Перник/Pernik, Хасково/Haskovo, Ямбол/Yambol. Always return city in canonical English form or null.
- Map genres to one of [Techno, House, HipHop, Pop, Rock, Jazz, Classical, Other] or null. техно=Techno, хаус=House, чалга=Pop, рок=Rock, джаз=Jazz.
- If query says 'около мен' or 'near me', set nearMe=true.
- Ignore filler words like 'искам', 'търся', 'покажи', 'намери', 'събитие', 'event'.
- dateIntent: short label such as 'tonight','tomorrow','this weekend','this week' or null.
- dateFrom/dateTo: ISO 8601 date strings or null.
- Return ONLY the JSON object.";

        private static string GetDescriptionSystemPrompt(string? lang) => (lang == "en")
            ? "You are a marketing copywriter for an events platform. Write a single concise event description (60-120 words) in plain English. Tone: energetic, inviting, modern. Do NOT invent artist names, prices, or times. Output ONLY the description text."
            : "Ти си маркетинг копирайтър за платформа за събития. Напиши кратко описание на събитието (60-120 думи) на естествен, жив български. Тон: енергичен, привлекателен, модерен. НЕ измисляй имена на артисти, цени или часове. Изведи САМО текста на описанието.";

        private readonly HttpClient _http;
        private readonly AiOptions _opts;
        private readonly ILogger<OpenAiService> _logger;

        public AiStatus LastStatus { get; private set; } = AiStatus.Disabled;
        public string? LastStatusDetail { get; private set; }

        public OpenAiService(HttpClient http, IOptions<AiOptions> opts, ILogger<OpenAiService> logger)
        {
            _http = http;
            _opts = opts.Value;
            _logger = logger;

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
            return InterpretInternalAsync(query, cancellationToken);
        }

        private async Task<AiSearchIntent?> InterpretInternalAsync(string query, CancellationToken cancellationToken)
        {
            var userContent = "User query: " + query.Trim() +
                              "\nCurrent UTC date: " + DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            var raw = await CallChatAsync(SearchSystemPrompt, userContent, 400, 0.2, cancellationToken);
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
            var raw = await CallChatAsync("You are a helpful assistant.", prompt, 800, 0.7, cancellationToken);
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

            var raw = await CallChatAsync(sys.ToString(), user, 300, 0.1, cancellationToken);
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

            var raw = await CallChatAsync(sys.ToString(), user.ToString(), 800, 0.6, cancellationToken);
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
            if (!string.IsNullOrWhiteSpace(hints)) sb.AppendLine("Extra notes: " + hints.Trim());

            var raw = await CallChatAsync(GetDescriptionSystemPrompt(lang), sb.ToString(), 200, 0.8, cancellationToken);
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

                if (TryParseDate(ReadString(r, "dateFrom"), out var df)) intent.DateFrom = df;
                if (TryParseDate(ReadString(r, "dateTo"), out var dt)) intent.DateTo = dt;

                if (intent.City == null && intent.Keyword == null && intent.Genre == null &&
                    intent.DateFrom == null && intent.DateTo == null && !intent.NearMe &&
                    intent.Keywords.Length == 0)
                    return null;

                return intent;
            }
            catch (JsonException)
            {
                return null;
            }
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

        private static string? NormalizeGenre(string s)
        {
            return s.Trim().ToLowerInvariant() switch
            {
                "техно" or "techno" => "Techno",
                "хаус" or "house" => "House",
                "хип хоп" or "хип-хоп" or "hip hop" or "hip-hop" or "hiphop" or "rap" or "рап" => "HipHop",
                "поп" or "pop" or "чалга" or "chalga" => "Pop",
                "рок" or "rock" => "Rock",
                "джаз" or "jazz" => "Jazz",
                "класика" or "classical" or "classic" => "Classical",
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
