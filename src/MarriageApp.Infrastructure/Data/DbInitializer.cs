using MarriageApp.Core.Enums;
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
    /// Applies pending migrations and seeds the "User"/"Admin" roles plus the admin accounts
    /// (one female, one male) read from the "Seed:Admins" config section. Women's (bride) photos
    /// may only be accessed by a FEMALE admin, hence each admin carries a gender.
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
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Read the configured admins; fall back to a sensible female + male pair.
        var admins = config.GetSection("Seed:Admins").GetChildren()
            .Select(c => (
                Email: c["Email"] ?? "",
                Password: c["Password"] ?? "Admin#12345",
                Gender: Enum.TryParse<Gender>(c["Gender"], out var g) ? g : Gender.Female,
                FullName: c["FullName"] ?? "مشرف النظام"))
            .Where(a => !string.IsNullOrWhiteSpace(a.Email))
            .ToList();

        if (admins.Count == 0)
        {
            admins.Add(("admin.female@marriageapp.local", "Admin#12345", Gender.Female, "المشرفة"));
            admins.Add(("admin.male@marriageapp.local", "Admin#12345", Gender.Male, "المشرف"));
        }

        foreach (var a in admins)
        {
            var existing = await userManager.FindByEmailAsync(a.Email);
            if (existing is null)
            {
                var admin = new ApplicationUser
                {
                    UserName = a.Email,
                    Email = a.Email,
                    EmailConfirmed = true,
                    FullName = a.FullName,
                    Gender = a.Gender,
                    CreatedAt = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(admin, a.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, AppRoles.Admin);
                    logger.LogInformation("Seeded {Gender} admin {Email}", a.Gender, a.Email);
                }
                else
                {
                    logger.LogError("Failed to seed admin {Email}: {Errors}", a.Email,
                        string.Join("; ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                // Ensure an existing admin has the right role + a gender (back-fill old seeds).
                if (!await userManager.IsInRoleAsync(existing, AppRoles.Admin))
                    await userManager.AddToRoleAsync(existing, AppRoles.Admin);
                if (existing.Gender is null)
                {
                    existing.Gender = a.Gender;
                    await userManager.UpdateAsync(existing);
                    logger.LogInformation("Back-filled gender {Gender} for admin {Email}", a.Gender, a.Email);
                }
            }
        }
    }
}
