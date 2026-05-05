using Npgsql;

namespace EventsApp.Infrastructure
{
    public static class DatabaseConnection
    {
        public static string GetPostgresConnectionString(IConfiguration configuration)
        {
            var databaseUrl = configuration["DATABASE_URL"];
            if (!string.IsNullOrWhiteSpace(databaseUrl))
            {
                return ConvertDatabaseUrl(databaseUrl);
            }

            return configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        private static string ConvertDatabaseUrl(string databaseUrl)
        {
            if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
            {
                throw new InvalidOperationException("DATABASE_URL must be a valid postgres:// or postgresql:// URL.");
            }

            var userInfo = uri.UserInfo.Split(':', 2);
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 5432,
                Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
                Username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty,
                Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
                Pooling = true,
            };

            var sslMode = GetQueryValue(uri.Query, "sslmode");
            if (!string.IsNullOrWhiteSpace(sslMode) &&
                Enum.TryParse<SslMode>(sslMode.Replace("-", string.Empty), ignoreCase: true, out var parsedSslMode))
            {
                builder.SslMode = parsedSslMode;
            }
            else
            {
                builder.SslMode = IsLocalHost(uri.Host) ? SslMode.Disable : SslMode.Require;
            }

            return builder.ConnectionString;
        }

        private static bool IsLocalHost(string host)
        {
            return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);
        }

        private static string? GetQueryValue(string query, string key)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return null;
            }

            foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = part.Split('=', 2);
                if (pair.Length == 2 &&
                    string.Equals(Uri.UnescapeDataString(pair[0]), key, StringComparison.OrdinalIgnoreCase))
                {
                    return Uri.UnescapeDataString(pair[1]);
                }
            }

            return null;
        }
    }
}
