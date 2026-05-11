using EventsApp.Data;
using EventsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Controllers.Api
{
    [ApiController]
    [Route("api/profiles")]
    [IgnoreAntiforgeryToken]
    public class ProfilesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfilesApiController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // GET /api/profiles/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> Details(string id)
        {
            var currentUserId = _userManager.GetUserId(User);

            var user = await _db.Users
                .AsNoTracking()
                .Include(u => u.Followers)
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                id = user.Id,
                userName = user.UserName,
                firstName = user.FirstName,
                lastName = user.LastName,
                profileImageUrl = user.ProfileImageUrl,
                bio = user.Bio,
                followerCount = user.Followers.Count,
                followingCount = user.Following.Count,
                isFollowing = currentUserId != null && user.Followers.Any(f => f.FollowerId == currentUserId),
                isOwnProfile = user.Id == currentUserId,
                roles = roles,
            });
        }

        // GET /api/profiles/me
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();
            return await Details(userId);
        }

        [HttpPost("{id}/follow")]
        [Authorize(Policy = "ApiAuth")]
        public async Task<IActionResult> Follow(string id)
        {
            var userId = _userManager.GetUserId(User)!;
            if (id == userId) return BadRequest(new { error = "Не можеш да следваш себе си." });
            if (!await _db.Users.AnyAsync(u => u.Id == id)) return NotFound();

            var exists = await _db.Follows.AnyAsync(f => f.FollowerId == userId && f.FollowingId == id);
            if (!exists)
            {
                _db.Follows.Add(new Follow { FollowerId = userId, FollowingId = id, CreatedAt = DateTime.UtcNow });
                await _db.SaveChangesAsync();
            }

            return Ok(new { isFollowing = true, followerCount = await _db.Follows.CountAsync(f => f.FollowingId == id) });
        }

        [HttpDelete("{id}/follow")]
        [Authorize(Policy = "ApiAuth")]
        public async Task<IActionResult> Unfollow(string id)
        {
            var userId = _userManager.GetUserId(User)!;
            var follow = await _db.Follows.FirstOrDefaultAsync(f => f.FollowerId == userId && f.FollowingId == id);
            if (follow != null)
            {
                _db.Follows.Remove(follow);
                await _db.SaveChangesAsync();
            }

            return Ok(new { isFollowing = false, followerCount = await _db.Follows.CountAsync(f => f.FollowingId == id) });
        }
    }
}
