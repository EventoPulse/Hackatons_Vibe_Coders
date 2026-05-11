using EventsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventsApp.Controllers.Api
{
    [ApiController]
    [Route("api/media")]
    [IgnoreAntiforgeryToken]
    [Authorize(Policy = "ApiAuth")]
    public class MediaApiController : ControllerBase
    {
        private readonly IMediaUploadService _media;

        public MediaApiController(IMediaUploadService media)
        {
            _media = media;
        }

        [HttpPost("upload")]
        [RequestSizeLimit(110L * 1024 * 1024)]
        public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string? folder, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0) return BadRequest(new { error = "Избери файл." });
            var subfolder = string.IsNullOrWhiteSpace(folder) ? "general" : folder.Trim();
            try
            {
                var result = await _media.SaveAsync(file, subfolder, cancellationToken);
                if (result == null) return BadRequest(new { error = "Файлът не може да бъде качен." });
                return Ok(new { url = result.Url, mediaType = result.MediaType.ToString() });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
