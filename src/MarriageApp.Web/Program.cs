using MarriageApp.Infrastructure;
using MarriageApp.Infrastructure.Data;
using MarriageApp.Infrastructure.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// ---- Infrastructure (EF Core, matching, notifications, photo storage) ----
builder.Services.AddInfrastructure(builder.Configuration);

// Persist Data Protection keys to a stable folder so encrypted photos stay decryptable
// and login cookies survive app restarts/redeploys (important on shared hosting).
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")))
    .SetApplicationName("SakinaApp");

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

// Apply migrations + seed roles and the two admins on startup.
await DbInitializer.SeedAsync(app.Services);

// Seed the sample grooms/brides in Development, or anywhere when Seed:DemoData is true
// (e.g. on the hosted demo). Idempotent — it skips accounts that already exist.
var seedDemoData = app.Configuration.GetValue<bool>("Seed:DemoData");
if (app.Environment.IsDevelopment() || seedDemoData)
{
    await TestDataSeeder.SeedAsync(app.Services);
}

app.Run();
