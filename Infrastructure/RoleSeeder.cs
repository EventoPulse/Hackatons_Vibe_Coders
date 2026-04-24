using EventsApp.Common;
using Microsoft.AspNetCore.Identity;

namespace EventsApp.Infrastructure
{
    public static class RoleSeeder
    {
        public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles = new[]
            {
                GlobalConstants.Roles.Admin,
                GlobalConstants.Roles.Organizer,
                GlobalConstants.Roles.User,
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}
