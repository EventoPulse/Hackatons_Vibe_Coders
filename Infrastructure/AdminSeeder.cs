using EventsApp.Common;
using EventsApp.Models;
using Microsoft.AspNetCore.Identity;

namespace EventsApp.Infrastructure
{
    public static class AdminSeeder
    {
        private const string DevelopmentAdminEmail = "admin@grooveon.com";
        private const string DevelopmentAdminUserName = "admin@grooveon.com";
        private const string DevelopmentAdminPassword = "admin";

        // Legacy email used in earlier builds - migrated to the configured admin email on startup.
        private const string LegacyAdminEmail = "admin@groooveon.com";

        public static async Task SeedAdminAsync(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var logger = serviceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(AdminSeeder));

            var adminEmail = FirstConfigured(
                configuration["Admin:Email"],
                configuration["ADMIN_EMAIL"],
                environment.IsDevelopment() ? DevelopmentAdminEmail : null);

            var adminPassword = FirstConfigured(
                configuration["Admin:Password"],
                configuration["ADMIN_PASSWORD"],
                environment.IsDevelopment() ? DevelopmentAdminPassword : null);

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                if (environment.IsDevelopment())
                {
                    logger.LogWarning("Admin seed skipped because admin credentials are missing.");
                    return;
                }

                throw new InvalidOperationException("Set ADMIN_EMAIL and ADMIN_PASSWORD before running Evento outside Development.");
            }

            var adminUserName = FirstConfigured(
                configuration["Admin:UserName"],
                configuration["ADMIN_USERNAME"],
                environment.IsDevelopment() ? DevelopmentAdminUserName : adminEmail)!;

            var admin = await userManager.FindByEmailAsync(adminEmail)
                        ?? await userManager.FindByEmailAsync(DevelopmentAdminEmail)
                        ?? await userManager.FindByEmailAsync(LegacyAdminEmail);

            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    EmailConfirmed = true,
                };

                var create = await userManager.CreateAsync(admin, adminPassword);
                if (!create.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Could not create admin user: " + string.Join("; ", create.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                admin.UserName = adminUserName;
                admin.NormalizedUserName = adminUserName.ToUpperInvariant();
                admin.Email = adminEmail;
                admin.NormalizedEmail = adminEmail.ToUpperInvariant();
                admin.EmailConfirmed = true;
                await userManager.UpdateAsync(admin);

                var shouldResetPassword = configuration.GetValue("Admin:ResetPasswordOnStartup", environment.IsDevelopment());
                if (shouldResetPassword)
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(admin);
                    var reset = await userManager.ResetPasswordAsync(admin, token, adminPassword);
                    if (!reset.Succeeded)
                    {
                        throw new InvalidOperationException(
                            "Could not reset admin password: " + string.Join("; ", reset.Errors.Select(e => e.Description)));
                    }
                }
            }

            if (!await userManager.IsInRoleAsync(admin, GlobalConstants.Roles.Admin))
            {
                await userManager.AddToRoleAsync(admin, GlobalConstants.Roles.Admin);
            }
        }

        private static string? FirstConfigured(params string?[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
        }
    }
}
