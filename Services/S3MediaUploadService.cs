using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using EventsApp.Models;
using Microsoft.AspNetCore.Http;

namespace EventsApp.Services
{
    public sealed class S3MediaUploadService : IMediaUploadService, IRemoteMediaService, IDisposable
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

        private readonly IAmazonS3 _client;
        private readonly ILogger<S3MediaUploadService> _logger;
        private readonly string _bucket;
        private readonly string _keyPrefix;
        private readonly int _signedUrlMinutes;

        public S3MediaUploadService(IConfiguration configuration, ILogger<S3MediaUploadService> logger)
        {
            _logger = logger;

            var endpoint = Read(configuration, "Media:S3:Endpoint", "S3_ENDPOINT", "ENDPOINT", "AWS_ENDPOINT_URL");
            _bucket = Read(configuration, "Media:S3:Bucket", "S3_BUCKET", "BUCKET", "AWS_S3_BUCKET", "AWS_S3_BUCKET_NAME") ?? string.Empty;
            var accessKey = Read(configuration, "Media:S3:AccessKeyId", "S3_ACCESS_KEY_ID", "S3_ACCESS_KEY", "ACCESS_KEY_ID", "AWS_ACCESS_KEY_ID");
            var secretKey = Read(configuration, "Media:S3:SecretAccessKey", "S3_SECRET_ACCESS_KEY", "S3_SECRET_KEY", "SECRET_ACCESS_KEY", "AWS_SECRET_ACCESS_KEY");
            var region = Read(configuration, "Media:S3:Region", "S3_REGION", "REGION", "AWS_REGION", "AWS_DEFAULT_REGION") ?? "auto";
            _keyPrefix = NormalizePrefix(Read(configuration, "Media:S3:KeyPrefix", "S3_KEY_PREFIX") ?? "uploads");
            _signedUrlMinutes = Math.Clamp(configuration.GetValue("Media:S3:SignedUrlMinutes", 30), 1, 60 * 24);

            if (string.IsNullOrWhiteSpace(endpoint) ||
                string.IsNullOrWhiteSpace(_bucket) ||
                string.IsNullOrWhiteSpace(accessKey) ||
                string.IsNullOrWhiteSpace(secretKey))
            {
                throw new InvalidOperationException(
                    "MEDIA_STORAGE=S3 is enabled, but S3/Railway bucket settings are incomplete. Required: endpoint, bucket, access key, secret key.");
            }

            var forcePathStyle = configuration.GetValue("Media:S3:ForcePathStyle", false);
            var s3Config = new AmazonS3Config
            {
                ServiceURL = endpoint,
                ForcePathStyle = forcePathStyle,
                AuthenticationRegion = region,
                RegionEndpoint = RegionEndpoint.GetBySystemName(region),
            };

            _client = new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), s3Config);
        }

        public async Task<MediaUploadResult?> SaveAsync(IFormFile file, string subfolder, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0) return null;

            var mediaType = ValidateMedia(file.FileName, file.ContentType, file.Length);
            var key = BuildStorageKey(subfolder, Path.GetExtension(file.FileName));

            await using var stream = file.OpenReadStream();
            await PutObjectAsync(key, stream, file.ContentType, cancellationToken);

            _logger.LogInformation("Stored S3 upload {Key} ({Bytes} bytes, {Type})", key, file.Length, mediaType);
            return new MediaUploadResult { Url = ToMediaUrl(key), MediaType = mediaType };
        }

        public async Task<MediaUploadResult?> SaveBytesAsync(byte[] data, string fileName, string subfolder, CancellationToken cancellationToken = default)
        {
            if (data == null || data.Length == 0) return null;

            var mediaType = ValidateMedia(fileName, null, data.Length, allowUnknownImage: true);
            var ext = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".png";

            var key = BuildStorageKey(subfolder, ext);
            await using var stream = new MemoryStream(data);
            await PutObjectAsync(key, stream, InferContentType(ext, mediaType), cancellationToken);

            _logger.LogInformation("Stored S3 byte upload {Key} ({Bytes} bytes, {Type})", key, data.Length, mediaType);
            return new MediaUploadResult { Url = ToMediaUrl(key), MediaType = mediaType };
        }

        public Task<string?> CreateReadUrlAsync(string mediaKey, CancellationToken cancellationToken = default)
        {
            var key = NormalizeMediaKey(mediaKey);
            if (string.IsNullOrWhiteSpace(key))
            {
                return Task.FromResult<string?>(null);
            }

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucket,
                Key = key,
                Expires = DateTime.UtcNow.AddMinutes(_signedUrlMinutes),
                Verb = HttpVerb.GET,
            };

            return Task.FromResult<string?>(_client.GetPreSignedURL(request));
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private async Task PutObjectAsync(string key, Stream stream, string? contentType, CancellationToken cancellationToken)
        {
            var request = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = stream,
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            };

            await _client.PutObjectAsync(request, cancellationToken);
        }

        private string BuildStorageKey(string subfolder, string extension)
        {
            var safeFolder = NormalizePathSegment(subfolder);
            var safeExt = string.IsNullOrWhiteSpace(extension) ? ".bin" : extension.ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{safeExt}";
            return string.IsNullOrWhiteSpace(_keyPrefix)
                ? $"{safeFolder}/{fileName}"
                : $"{_keyPrefix}/{safeFolder}/{fileName}";
        }

        private static PostMediaType ValidateMedia(string fileName, string? contentType, long length, bool allowUnknownImage = false)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            PostMediaType mediaType;
            long maxBytes;

            if (ImageExtensions.Contains(ext) || (allowUnknownImage && string.IsNullOrWhiteSpace(ext)))
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

            if (!string.IsNullOrWhiteSpace(contentType) && !AllowedContentTypes.Contains(contentType))
            {
                throw new InvalidOperationException($"Unsupported content type: {contentType}");
            }

            if (length > maxBytes)
            {
                throw new InvalidOperationException(
                    $"File too large. Max for {mediaType.ToString().ToLower()} is {maxBytes / (1024 * 1024)} MB.");
            }

            return mediaType;
        }

        private static string InferContentType(string extension, PostMediaType mediaType)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".mov" => "video/quicktime",
                ".m4v" => "video/x-m4v",
                _ => mediaType == PostMediaType.Video ? "video/mp4" : "image/png",
            };
        }

        private static string NormalizePathSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.Contains("..", StringComparison.Ordinal) ||
                value.Contains("\\", StringComparison.Ordinal) ||
                value.StartsWith("/", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Invalid upload folder.");
            }

            var safe = value.Trim('/').Replace("//", "/", StringComparison.Ordinal);
            return string.IsNullOrWhiteSpace(safe) ? throw new InvalidOperationException("Invalid upload folder.") : safe;
        }

        private string NormalizeMediaKey(string mediaKey)
        {
            if (string.IsNullOrWhiteSpace(mediaKey) ||
                mediaKey.Contains("..", StringComparison.Ordinal) ||
                mediaKey.Contains("\\", StringComparison.Ordinal) ||
                mediaKey.StartsWith("/", StringComparison.Ordinal))
            {
                return string.Empty;
            }

            var key = Uri.UnescapeDataString(mediaKey).Trim('/');
            if (string.IsNullOrWhiteSpace(_keyPrefix))
            {
                return key;
            }

            return key.StartsWith(_keyPrefix + "/", StringComparison.Ordinal)
                ? key
                : string.Empty;
        }

        private static string NormalizePrefix(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            if (value.Contains("..", StringComparison.Ordinal) || value.Contains("\\", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Invalid S3 key prefix.");
            }

            return value.Trim('/');
        }

        private static string ToMediaUrl(string key) => "/media/" + key;

        private static string? Read(IConfiguration configuration, params string[] keys)
        {
            foreach (var key in keys)
            {
                var value = configuration[key];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return null;
        }
    }
}
