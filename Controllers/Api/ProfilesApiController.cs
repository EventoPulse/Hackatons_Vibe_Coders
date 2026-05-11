using EventsApp.Data;
using EventsApp.Models;
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
    }
}
