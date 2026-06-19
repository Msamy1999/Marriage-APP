using MarriageApp.Core.Entities;
using MarriageApp.Core.Enums;
using MarriageApp.Core.Services;
using MarriageApp.Infrastructure.Data;
using MarriageApp.Infrastructure.Identity;
using MarriageApp.Web.Mapping;
using MarriageApp.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarriageApp.Web.Controllers;

/// <summary>Applicant-facing profile completion, dashboard, and photo upload.</summary>
[Authorize(Roles = AppRoles.User)]
public class ProfileController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPhotoStorageService _photoStorage;

    private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/webp" };
    private const long MaxPhotoBytes = 5 * 1024 * 1024; // 5 MB

    public ProfileController(AppDbContext db, UserManager<ApplicationUser> userManager, IPhotoStorageService photoStorage)
    {
        _db = db;
        _userManager = userManager;
        _photoStorage = photoStorage;
    }

    private async Task<Profile?> LoadMyProfileAsync()
    {
        var userId = _userManager.GetUserId(User);
        return await _db.Profiles
            .Include(p => p.FamilyDetails)
            .Include(p => p.Requirements).ThenInclude(r => r!.AcceptedResidences)
            .Include(p => p.Photos)
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    // ---- Dashboard: status + reassurance message ----
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var profile = await LoadMyProfileAsync();
        if (profile is null) return RedirectToAction(nameof(Edit));
        return View(profile);
    }

    // ---- Create/Edit the detailed application form ----
    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var profile = await LoadMyProfileAsync();
        if (profile is null) return RedirectToAction("Index", "Home");
        return View(ProfileMapper.ToViewModel(profile));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileFormViewModel model)
    {
        var profile = await LoadMyProfileAsync();
        if (profile is null) return RedirectToAction("Index", "Home");

        // Gender is fixed at registration; ignore any tampering from the form.
        model.Gender = profile.Gender;

        if (!ModelState.IsValid) return View(model);

        ProfileMapper.Apply(model, profile, DateTime.UtcNow);

        // Completing the form moves an incomplete profile into the matching pool.
        if (profile.Status == ProfileStatus.Incomplete)
            profile.Status = ProfileStatus.PendingMatch;

        await _db.SaveChangesAsync();
        TempData["Message"] = "تم حفظ بياناتك بنجاح. شكراً لك، سيصلك إشعار عند إيجاد شريك مناسب.";
        return RedirectToAction(nameof(Dashboard));
    }

    // ---- Photo upload ----
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPhoto(IFormFile photo, PhotoVisibility visibility)
    {
        var profile = await LoadMyProfileAsync();
        if (profile is null) return RedirectToAction("Index", "Home");

        if (photo is null || photo.Length == 0)
        {
            TempData["Error"] = "لم يتم اختيار صورة.";
            return RedirectToAction(nameof(Dashboard));
        }
        if (photo.Length > MaxPhotoBytes || !AllowedImageTypes.Contains(photo.ContentType))
        {
            TempData["Error"] = "صيغة الصورة غير مدعومة أو الحجم أكبر من 5 ميجابايت.";
            return RedirectToAction(nameof(Dashboard));
        }

        using var ms = new MemoryStream();
        await photo.CopyToAsync(ms);
        var blobPath = await _photoStorage.SaveEncryptedAsync(ms.ToArray(), photo.ContentType);

        _db.ProfilePhotos.Add(new ProfilePhoto
        {
            ProfileId = profile.Id,
            EncryptedBlobPath = blobPath,
            ContentType = photo.ContentType,
            OriginalFileName = Path.GetFileName(photo.FileName),
            Visibility = visibility,
            IsPrimary = !profile.Photos.Any(),
            UploadedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        TempData["Message"] = "تم رفع الصورة بأمان.";
        return RedirectToAction(nameof(Dashboard));
    }
}
