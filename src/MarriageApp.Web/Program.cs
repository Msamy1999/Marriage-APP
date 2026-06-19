using MarriageApp.Infrastructure;
using MarriageApp.Infrastructure.Data;
using MarriageApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// ---- Infrastructure (EF Core, matching, notifications, photo storage) ----
builder.Services.AddInfrastructure(builder.Configuration);

// ---- ASP.NET Core Identity with role support ("User" / "Admin") ----
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Custom auth endpoints live in AccountController, so point the cookie there.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Apply migrations + seed roles and the default admin on startup.
await DbInitializer.SeedAsync(app.Services);

// In Development only: seed sample grooms/brides (idempotent — skips if profiles exist).
if (app.Environment.IsDevelopment())
{
    await TestDataSeeder.SeedAsync(app.Services);
}

app.Run();
