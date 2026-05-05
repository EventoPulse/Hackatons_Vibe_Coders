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
        Task<MediaUploadResult?> SaveBytesAsync(byte[] data, string fileName, string subfolder, CancellationToken cancellationToken = default);
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

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/gif", "image/webp",
            "video/mp4", "video/webm", "video/quicktime", "video/x-m4v",
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

            if (!string.IsNullOrWhiteSpace(file.ContentType) && !AllowedContentTypes.Contains(file.ContentType))
            {
                throw new InvalidOperationException($"Unsupported content type: {file.ContentType}");
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

            var dir = ResolveUploadDirectory(webRoot, subfolder);
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

        public async Task<MediaUploadResult?> SaveBytesAsync(byte[] data, string fileName, string subfolder, CancellationToken cancellationToken = default)
        {
            if (data == null || data.Length == 0) return null;

            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            PostMediaType mediaType;

            if (ImageExtensions.Contains(ext))
            {
                mediaType = PostMediaType.Image;
                if (data.Length > MaxImageBytes) throw new InvalidOperationException($"File too large. Max for image is {MaxImageBytes / (1024 * 1024)} MB.");
            }
            else if (VideoExtensions.Contains(ext))
            {
                mediaType = PostMediaType.Video;
                if (data.Length > MaxVideoBytes) throw new InvalidOperationException($"File too large. Max for video is {MaxVideoBytes / (1024 * 1024)} MB.");
            }
            else
            {
                // default to image if unknown
                mediaType = PostMediaType.Image;
            }

            var webRoot = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
            {
                webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
            }

            var dir = ResolveUploadDirectory(webRoot, subfolder);
            Directory.CreateDirectory(dir);

            var safeExt = ext;
            if (string.IsNullOrWhiteSpace(safeExt)) safeExt = ".png";
            var fileBase = Path.GetFileNameWithoutExtension(fileName);
            var fileNameFinal = $"{Guid.NewGuid():N}{safeExt}";
            var fullPath = Path.Combine(dir, fileNameFinal);

            await File.WriteAllBytesAsync(fullPath, data, cancellationToken);

            var url = $"/uploads/{subfolder}/{fileNameFinal}";
            _logger.LogInformation("Stored upload (bytes) {Url} ({Bytes} bytes, {Type})", url, data.Length, mediaType);
            return new MediaUploadResult { Url = url, MediaType = mediaType };
        }

        private static string ResolveUploadDirectory(string webRoot, string subfolder)
        {
            if (string.IsNullOrWhiteSpace(subfolder) ||
                subfolder.Contains("..", StringComparison.Ordinal) ||
                subfolder.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                throw new InvalidOperationException("Invalid upload folder.");
            }

            var uploadsRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads"));
            var uploadDir = Path.GetFullPath(Path.Combine(uploadsRoot, subfolder));

            if (!uploadDir.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Invalid upload folder.");
            }

            return uploadDir;
        }
    }
}
