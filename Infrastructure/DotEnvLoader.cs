namespace EventsApp.Infrastructure
{
    public static class DotEnvLoader
    {
        public static IDictionary<string, string> LoadIntoConfiguration(string envPath, IConfigurationBuilder configBuilder)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(envPath))
            {
                return dict;
            }

            foreach (var line in File.ReadAllLines(envPath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("#")) continue;

                var idx = line.IndexOf('=');
                if (idx <= 0) continue;

                var key = line.Substring(0, idx).Trim();
                var value = line.Substring(idx + 1).Trim();

                if (value.Length >= 2 &&
                    ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
                {
                    value = value.Substring(1, value.Length - 2);
                }

                dict[key] = value;
            }

            var mapped = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in dict)
            {
                mapped[kv.Key] = kv.Value;

                if (string.Equals(kv.Key, "OPENAI_API_KEY", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "Sirma_key", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "SIRMA_API_KEY", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "AI_API_KEY", StringComparison.OrdinalIgnoreCase))
                {
                    mapped["AI:ApiKey"] = kv.Value;
                    mapped["SirmaAi:ApiKey"] = kv.Value;
                }
                if (string.Equals(kv.Key, "Sirma_project_id", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "SIRMA_PROJECT_ID", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "AI_PROJECT_ID", StringComparison.OrdinalIgnoreCase))
                {
                    mapped["AI:ProjectId"] = kv.Value;
                }
                if (string.Equals(kv.Key, "Sirma_agent_id", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "SIRMA_AGENT_ID", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "AI_AGENT_ID", StringComparison.OrdinalIgnoreCase))
                {
                    mapped["AI:AgentId"] = kv.Value;
                    mapped["SirmaAi:AgentId"] = kv.Value;
                }
                if (string.Equals(kv.Key, "Sirma_domain", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "SIRMA_DOMAIN", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "SIRMA_AI_DOMAIN", StringComparison.OrdinalIgnoreCase))
                {
                    mapped["SirmaAi:Domain"] = kv.Value;
                }
                if (string.Equals(kv.Key, "GOOGLE_MAPS_KEY", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "GOOGLE_MAPS_API_KEY", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "GoogleMaps_key", StringComparison.OrdinalIgnoreCase))
                {
                    mapped["GoogleMaps:ApiKey"] = kv.Value;
                }
                if (string.Equals(kv.Key, "SMTP_HOST", StringComparison.OrdinalIgnoreCase))
                {
                    mapped["Email:Smtp:Host"] = kv.Value;
                    mapped["Email:Enabled"] = "true";
                }
                if (string.Equals(kv.Key, "SMTP_PORT", StringComparison.OrdinalIgnoreCase))
                {
                    mapped["Email:Smtp:Port"] = kv.Value;
                }
                if (string.Equals(kv.Key, "SMTP_USERNAME", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "SMTP_USER", StringComparison.OrdinalIgnoreCase))
                {
                    mapped["Email:Smtp:Username"] = kv.Value;
                }
                if (string.Equals(kv.Key, "SMTP_PASSWORD", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "SMTP_PASS", StringComparison.OrdinalIgnoreCase))
                {
                    mapped["Email:Smtp:Password"] = kv.Value;
                }
                if (string.Equals(kv.Key, "SMTP_FROM_EMAIL", StringComparison.OrdinalIgnoreCase))
                {
                    mapped["Email:From:Email"] = kv.Value;
                }
                if (string.Equals(kv.Key, "SMTP_ENABLE_SSL", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "SMTP_SSL", StringComparison.OrdinalIgnoreCase))
                {
                    mapped["Email:Smtp:EnableSsl"] = kv.Value;
                }
                if (string.Equals(kv.Key, "EMAIL_ENABLED", StringComparison.OrdinalIgnoreCase))
                {
                    mapped["Email:Enabled"] = kv.Value;
                }
                if (string.Equals(kv.Key, "SMTP_FROM_NAME", StringComparison.OrdinalIgnoreCase))
                {
                    mapped["Email:From:Name"] = kv.Value;
                }
                if (string.Equals(kv.Key, "APP_PUBLIC_URL", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "PUBLIC_URL", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "BASE_URL", StringComparison.OrdinalIgnoreCase))
                {
                    mapped["App:PublicUrl"] = kv.Value;
                }
                if (string.Equals(kv.Key, "GOOGLE_SITE_VERIFICATION", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "GOOGLE_SEARCH_CONSOLE_VERIFICATION", StringComparison.OrdinalIgnoreCase))
                {
                    mapped["Google:SiteVerification"] = kv.Value;
                }
                if (string.Equals(kv.Key, "MEDIA_STORAGE", StringComparison.OrdinalIgnoreCase))
                {
                    mapped["Media:Storage"] = kv.Value;
                }
                if (string.Equals(kv.Key, "S3_ENDPOINT", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "ENDPOINT", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "AWS_ENDPOINT_URL", StringComparison.OrdinalIgnoreCase))
                {
                    mapped["Media:S3:Endpoint"] = kv.Value;
                }
                if (string.Equals(kv.Key, "S3_BUCKET", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "BUCKET", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "AWS_S3_BUCKET", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "AWS_S3_BUCKET_NAME", StringComparison.OrdinalIgnoreCase))
                {
                    mapped["Media:S3:Bucket"] = kv.Value;
                }
                if (string.Equals(kv.Key, "S3_ACCESS_KEY_ID", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "S3_ACCESS_KEY", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "ACCESS_KEY_ID", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "AWS_ACCESS_KEY_ID", StringComparison.OrdinalIgnoreCase))
                {
                    mapped["Media:S3:AccessKeyId"] = kv.Value;
                }
                if (string.Equals(kv.Key, "S3_SECRET_ACCESS_KEY", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "S3_SECRET_KEY", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "SECRET_ACCESS_KEY", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "AWS_SECRET_ACCESS_KEY", StringComparison.OrdinalIgnoreCase))
                {
                    mapped["Media:S3:SecretAccessKey"] = kv.Value;
                }
                if (string.Equals(kv.Key, "S3_REGION", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "REGION", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "AWS_REGION", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "AWS_DEFAULT_REGION", StringComparison.OrdinalIgnoreCase))
                {
                    mapped["Media:S3:Region"] = kv.Value;
                }
                if (string.Equals(kv.Key, "S3_FORCE_PATH_STYLE", StringComparison.OrdinalIgnoreCase))
                {
                    mapped["Media:S3:ForcePathStyle"] = kv.Value;
                }
                if (string.Equals(kv.Key, "S3_KEY_PREFIX", StringComparison.OrdinalIgnoreCase))
                {
                    mapped["Media:S3:KeyPrefix"] = kv.Value;
                }
                if (string.Equals(kv.Key, "S3_SIGNED_URL_MINUTES", StringComparison.OrdinalIgnoreCase))
                {
                    mapped["Media:S3:SignedUrlMinutes"] = kv.Value;
                }
            }

            configBuilder.AddInMemoryCollection(mapped);
            return dict;
        }
    }
}
