using EventsApp.Models;
using Microsoft.AspNetCore.Http;

namespace EventsApp.Services
{
    public class MediaUploadResult
    {
        public string Url { get; set; } = null!;
        public PostMediaType MediaType { get; set; }
    }

    public interface IMediaUploadService
    {
        Task<MediaUploadResult?> SaveAsync(IFormFile file, string subfolder, CancellationToken cancellationToken = default);
    }

    public class MediaUploadService : IMediaUploadService
    {
        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp",
        };

        private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".webm", ".mov", ".m4v",
        };

        private const long MaxImageBytes = 5L * 1024 * 1024;
        private const long MaxVideoBytes = 100L * 1024 * 1024;

        private readonly IWebHostEnvironment _env;
        private readonly ILogger<MediaUploadService> _logger;

        public MediaUploadService(IWebHostEnvironment env, ILogger<MediaUploadService> logger)
        {
            _env = env;
            _logger = logger;
        }

        public async Task<MediaUploadResult?> SaveAsync(IFormFile file, string subfolder, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0) return null;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            PostMediaType mediaType;
            long maxBytes;

            if (ImageExtensions.Contains(ext))
            {
                mediaType = PostMediaType.Image;
                maxBytes = MaxImageBytes;
            }
            else if (VideoExtensions.Contains(ext))
            {
                mediaType = PostMediaType.Video;
                maxBytes = MaxVideoBytes;
            }
            else
            {
                throw new InvalidOperationException($"Unsupported file type: {ext}");
            }

            if (file.Length > maxBytes)
            {
                throw new InvalidOperationException(
                    $"File too large. Max for {mediaType.ToString().ToLower()} is {maxBytes / (1024 * 1024)} MB.");
            }

            var webRoot = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
            {
                webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
            }

            var dir = Path.Combine(webRoot, "uploads", subfolder);
            Directory.CreateDirectory(dir);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(dir, fileName);

            await using (var stream = File.Create(fullPath))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            var url = $"/uploads/{subfolder}/{fileName}";
            _logger.LogInformation("Stored upload {Url} ({Bytes} bytes, {Type})", url, file.Length, mediaType);

            return new MediaUploadResult { Url = url, MediaType = mediaType };
        }
    }
}
