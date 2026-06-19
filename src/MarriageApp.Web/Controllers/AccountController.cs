using MarriageApp.Core.Entities;
using MarriageApp.Core.Enums;
using MarriageApp.Infrastructure.Data;
using MarriageApp.Infrastructure.Identity;
using MarriageApp.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MarriageApp.Web.Controllers;

/// <summary>Registration, login, and logout for both applicants and admins.</summary>
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AppDbContext _db;

    public AccountController(UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager, AppDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
    }

    // ---- Registration (applicants) ----
    // Gender comes from the landing-page selection and pre-seeds the form.
    [HttpGet]
    public IActionResult Register(Gender gender = Gender.Male)
        => View(new RegisterViewModel { Gender = gender });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, AppRoles.User);

        // Create an empty profile carrying the chosen gender; the user completes it next.
        _db.Profiles.Add(new Profile
        {
            UserId = user.Id,
            Gender = model.Gender,
            Name = model.FullName,
            CurrentResidence = string.Empty,
            PhoneNumber = string.Empty,
            Status = ProfileStatus.Incomplete,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        await _signInManager.SignInAsync(user, isPersistent: false);
        // Straight into completing the detailed application form.
        return RedirectToAction("Edit", "Profile");
    }

    // ---- Login (shared by users and admins; redirect depends on role) ----
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "بيانات الدخول غير صحيحة");
            return View(model);
        }

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is not null && await _userManager.IsInRoleAsync(user, AppRoles.Admin))
            return RedirectToAction("Index", "Admin");

        return RedirectToAction("Dashboard", "Profile");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
