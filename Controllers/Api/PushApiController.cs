using System.Text.Json.Serialization;
using EventsApp.Data;
using EventsApp.Models;
using EventsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Controllers.Api
{
    [ApiController]
    [Route("api/push")]
    [IgnoreAntiforgeryToken]
    [Authorize(Policy = "ApiAuth")]
    public class PushApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPushNotificationService _pushNotifications;

        public PushApiController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IPushNotificationService pushNotifications)
        {
            _db = db;
            _userManager = userManager;
            _pushNotifications = pushNotifications;
        }

        [HttpGet("public-key")]
        public IActionResult PublicKey()
        {
            if (!_pushNotifications.IsConfigured || string.IsNullOrWhiteSpace(_pushNotifications.PublicKey))
            {
                return NotFound(new { error = "push_not_configured" });
            }

            return Ok(new { publicKey = _pushNotifications.PublicKey });
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] PushSubscribeRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)
                || string.IsNullOrWhiteSpace(request.Endpoint)
                || string.IsNullOrWhiteSpace(request.Keys?.P256DH)
                || string.IsNullOrWhiteSpace(request.Keys?.Auth))
            {
                return BadRequest(new { error = "invalid_push_subscription" });
            }

            var now = DateTime.UtcNow;
            var subscription = await _db.UserPushSubscriptions
                .FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint);

            if (subscription == null)
            {
                subscription = new UserPushSubscription
                {
                    UserId = userId,
                    Endpoint = request.Endpoint,
                    CreatedAt = now,
                };
                _db.UserPushSubscriptions.Add(subscription);
            }
            else if (subscription.UserId != userId)
            {
                subscription.UserId = userId;
            }

            subscription.P256DH = request.Keys.P256DH;
            subscription.Auth = request.Keys.Auth;
            subscription.UserAgent = Request.Headers.UserAgent.ToString();
            subscription.LastSeenAt = now;

            await _db.SaveChangesAsync();

            return Ok(new { ok = true });
        }

        [HttpPost("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] PushUnsubscribeRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(request.Endpoint))
            {
                return BadRequest(new { error = "invalid_push_subscription" });
            }

            await _db.UserPushSubscriptions
                .Where(s => s.UserId == userId && s.Endpoint == request.Endpoint)
                .ExecuteDeleteAsync();

            return Ok(new { ok = true });
        }

        public class PushSubscribeRequest
        {
            public string Endpoint { get; set; } = string.Empty;

            public PushSubscriptionKeys? Keys { get; set; }
        }

        public class PushSubscriptionKeys
        {
            [JsonPropertyName("p256dh")]
            public string P256DH { get; set; } = string.Empty;

            [JsonPropertyName("auth")]
            public string Auth { get; set; } = string.Empty;
        }

        public class PushUnsubscribeRequest
        {
            public string Endpoint { get; set; } = string.Empty;
        }
    }
}
