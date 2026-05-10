using System.Security.Cryptography;
using System.Text;
using EventsApp.Services.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;

namespace EventsApp.Controllers.Api
{
    [ApiController]
    [Route("api/translate")]
    [EnableRateLimiting("ai-light")]
    public class TranslateApiController : ControllerBase
    {
        private const int MaxTextLength = 1400;

        private readonly IAiSearchService _ai;
        private readonly IMemoryCache _cache;
        private readonly ILogger<TranslateApiController> _logger;

        public TranslateApiController(IAiSearchService ai, IMemoryCache cache, ILogger<TranslateApiController> logger)
        {
            _ai = ai;
            _cache = cache;
            _logger = logger;
        }

        public sealed class TranslateRequest
        {
            public string? Text { get; set; }
            public string? TargetLanguage { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Translate([FromBody] TranslateRequest? request, CancellationToken cancellationToken)
        {
            var text = (request?.Text ?? string.Empty).Trim();
            var target = NormalizeTargetLanguage(request?.TargetLanguage);

            if (string.IsNullOrWhiteSpace(text))
            {
                return Ok(new { translatedText = string.Empty, cached = true });
            }

            if (text.Length > MaxTextLength)
            {
                text = text[..MaxTextLength];
            }

            if (!_ai.IsEnabled)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    message = "AI translation is not configured.",
                });
            }

            var cacheKey = "translate:" + target + ":" + Sha256(text);
            if (_cache.TryGetValue(cacheKey, out string? cached) && !string.IsNullOrWhiteSpace(cached))
            {
                return Ok(new { translatedText = cached, cached = true });
            }

            var languageName = target == "bg" ? "Bulgarian" : "English";
            var prompt = $"""
                Translate the following event-platform text to {languageName}.
                Preserve @mentions, hashtags, URLs, emoji, dates, prices, venue names and line breaks.
                Do not add commentary. Output ONLY the translated text.

                Text:
                {text}
                """;

            try
            {
                var translated = await _ai.GenerateTextAsync(prompt, "translate", cancellationToken);
                if (string.IsNullOrWhiteSpace(translated))
                {
                    return StatusCode(StatusCodes.Status502BadGateway, new { message = "Translation failed." });
                }

                translated = translated.Trim();
                _cache.Set(cacheKey, translated, TimeSpan.FromDays(7));
                return Ok(new { translatedText = translated, cached = false });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Translation failed");
                return StatusCode(StatusCodes.Status502BadGateway, new { message = "Translation failed." });
            }
        }

        private static string NormalizeTargetLanguage(string? target)
        {
            return string.Equals(target, "bg", StringComparison.OrdinalIgnoreCase) ? "bg" : "en";
        }

        private static string Sha256(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes);
        }
    }
}
