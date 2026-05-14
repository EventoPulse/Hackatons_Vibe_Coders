namespace EventsApp.Services
{
    public interface IAppLinkService
    {
        string ToAbsoluteUrl(HttpRequest request, string? pathAndQuery);
    }

    public class AppLinkService : IAppLinkService
    {
        private readonly IConfiguration _configuration;

        public AppLinkService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string ToAbsoluteUrl(HttpRequest request, string? pathAndQuery)
        {
            var isBackendOnlyLink = IsBackendOnlyLink(pathAndQuery);

            /*
             * IMPORTANT:
             * APP_PUBLIC_URL can point to the frontend domain, for example:
             * https://evento.business
             *
             * That is OK for frontend links, but it breaks backend-only links like:
             * /email/confirm/{token}
             *
             * Email confirmation is handled by ASP.NET backend routes, not by the frontend.
             * Therefore backend-only links must prefer the backend public URL / Railway URL / request host.
             */
            var configuredUrl = isBackendOnlyLink
                ? GetSetting(
                    "BACKEND_PUBLIC_URL",
                    "API_PUBLIC_URL",
                    "RAILWAY_PUBLIC_DOMAIN",
                    "RAILWAY_STATIC_URL")
                : GetSetting(
                    "APP_PUBLIC_URL",
                    "PUBLIC_URL",
                    "App:PublicUrl",
                    "BACKEND_PUBLIC_URL",
                    "API_PUBLIC_URL",
                    "RAILWAY_PUBLIC_DOMAIN",
                    "RAILWAY_STATIC_URL");

            var baseUrl = BuildBaseUrl(configuredUrl, request).TrimEnd('/') + "/";

            if (string.IsNullOrWhiteSpace(pathAndQuery))
            {
                return baseUrl.TrimEnd('/');
            }

            if (Uri.TryCreate(pathAndQuery, UriKind.Absolute, out var absolute))
            {
                if (string.Equals(absolute.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(absolute.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                {
                    return absolute.ToString();
                }

                pathAndQuery = string.IsNullOrWhiteSpace(absolute.PathAndQuery)
                    ? "/"
                    : absolute.PathAndQuery;
            }

            return new Uri(new Uri(baseUrl), pathAndQuery.TrimStart('/')).ToString();
        }

        private string? GetSetting(params string[] keys)
        {
            foreach (var key in keys)
            {
                var value = _configuration[key];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }

        private string BuildBaseUrl(string? configuredUrl, HttpRequest request)
        {
            var normalizedConfiguredUrl = NormalizePublicUrl(configuredUrl);
            if (!string.IsNullOrWhiteSpace(normalizedConfiguredUrl))
            {
                return normalizedConfiguredUrl;
            }

            var railwayDomain = NormalizePublicUrl(GetSetting("RAILWAY_PUBLIC_DOMAIN", "RAILWAY_STATIC_URL"));
            if (!string.IsNullOrWhiteSpace(railwayDomain))
            {
                return railwayDomain;
            }

            var scheme = request.Scheme;
            if (!string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                scheme = Uri.UriSchemeHttps;
            }

            if (request.Host.HasValue)
            {
                return $"{scheme}://{request.Host.Value}";
            }

            return "https://evento.business";
        }

        private static bool IsBackendOnlyLink(string? pathAndQuery)
        {
            if (string.IsNullOrWhiteSpace(pathAndQuery))
            {
                return false;
            }

            if (Uri.TryCreate(pathAndQuery, UriKind.Absolute, out var absolute))
            {
                pathAndQuery = absolute.PathAndQuery;
            }

            var path = pathAndQuery.Trim();

            return path.StartsWith("/email/confirm", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("email/confirm", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/confirm-email", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("confirm-email", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/account/confirm-email", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("account/confirm-email", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/Identity/Account/ConfirmEmail", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("Identity/Account/ConfirmEmail", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/reset-password", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("reset-password", StringComparison.OrdinalIgnoreCase);
        }

        private static string? NormalizePublicUrl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim().TrimEnd('/');
            if (!trimmed.Contains("://", StringComparison.Ordinal))
            {
                trimmed = "https://" + trimmed;
            }

            if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) &&
                !string.IsNullOrWhiteSpace(uri.Host) &&
                (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            {
                return uri.ToString().TrimEnd('/');
            }

            return null;
        }
    }
}
