using MarriageApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MarriageApp.Infrastructure.Data;

/// <summary>Role/admin seeding constants and bootstrapper.</summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string User = "User";
}

public static class DbInitializer
{
    /// <summary>
    /// Applies pending migrations and seeds the "User"/"Admin" roles plus a default admin
    /// account (credentials read from the "Seed:Admin" config section).
    /// </summary>
    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        var db = services.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { AppRoles.Admin, AppRoles.User })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var config = services.GetRequiredService<IConfiguration>();
        var adminEmail = config["Seed:Admin:Email"] ?? "admin@marriageapp.local";
        var adminPassword = config["Seed:Admin:Password"] ?? "Admin#12345";

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "مشرف النظام",
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, AppRoles.Admin);
                logger.LogInformation("Seeded default admin {Email}", adminEmail);
            }
            else
            {
                logger.LogError("Failed to seed admin: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
