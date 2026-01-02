using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicketMS.Infrastructure.Data;
using TicketMS.Infrastructure.Entities;

namespace TicketMS.Infrastructure.Seed
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            // Apply migrations
            await context.Database.MigrateAsync();

            // Seed roles
            await SeedRolesAsync(roleManager);

            // Seed admin user
            await SeedAdminUserAsync(userManager);
        }

        private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
        {
            var roles = new List<(string Name, string Description)>
        {
            ("Admin", "Full system access"),
            ("ProjectManager", "Can manage projects, sprints, and team members"),
            ("Developer", "Can create and manage tickets"),
            ("QA", "Can create bugs and verify fixes"),
            ("Viewer", "Read-only access")
        };

            foreach (var (name, description) in roles)
            {
                if (!await roleManager.RoleExistsAsync(name))
                {
                    await roleManager.CreateAsync(new ApplicationRole
                    {
                        Name = name,
                        Description = description
                    });
                }
            }
        }

        private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
        {
            const string adminEmail = "elan@gmail.com";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Elan",
                    LastName = "P",
                    DateOfBirth = new DateTime(1999, 8, 15),
                    PhoneNumber = "9876543210", 
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(admin, "Admin@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }
        }
    }
}