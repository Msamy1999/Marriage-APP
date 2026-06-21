using MarriageApp.Core.Entities;
using MarriageApp.Core.Enums;
using MarriageApp.Core.Services;
using MarriageApp.Infrastructure.Data;
using MarriageApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarriageApp.Web.Controllers;

/// <summary>
/// Streams profile photos through an authorization gate. Photos are NEVER served as
/// static files — every request runs the access check below and is written to
/// <see cref="PhotoAccessLog"/>. Access rules:
///   • Women's (bride) photos -> only a FEMALE admin may view AND download them.
///     A male admin is denied (and the attempt is logged).
///   • Men's (groom) photos -> any admin may view AND download.
///   • A matched groom -> may VIEW (not download) the bride's photos only after a FEMALE
///     admin has explicitly REVEALED them (Match.PhotosRevealedToGroom) and the bride's
///     own <see cref="PhotoVisibility"/> permits it.
///   • The owner -> may view their own photos.
/// </summary>
[Authorize]
public class PhotoController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPhotoStorageService _storage;

    public PhotoController(AppDbContext db, UserManager<ApplicationUser> userManager, IPhotoStorageService storage)
    {
        _db = db;
        _userManager = userManager;
        _storage = storage;
    }

    [HttpGet]
    public Task<IActionResult> View(int id) => ServeAsync(id, asDownload: false);

    [HttpGet]
    public Task<IActionResult> Download(int id) => ServeAsync(id, asDownload: true);

    private async Task<IActionResult> ServeAsync(int photoId, bool asDownload)
    {
        var viewer = await _userManager.GetUserAsync(User);
        if (viewer is null) return Forbid();
        var userId = viewer.Id;
        var isAdmin = User.IsInRole(AppRoles.Admin);

        var photo = await _db.ProfilePhotos
            .Include(p => p.Profile)
            .FirstOrDefaultAsync(p => p.Id == photoId);

        if (photo is null) return NotFound();

        var (allowedView, allowedDownload) = await EvaluateAccessAsync(photo, viewer, isAdmin);
        var wants = asDownload ? allowedDownload : allowedView;

        if (!wants)
        {
            await LogAsync(photoId, userId, PhotoAccessAction.Denied);
            return Forbid();
        }

        await LogAsync(photoId, userId, asDownload ? PhotoAccessAction.Download : PhotoAccessAction.View);

        var data = await _storage.ReadDecryptedAsync(photo.EncryptedBlobPath, photo.ContentType, photo.OriginalFileName);
        if (asDownload)
            return File(data.Content, data.ContentType, data.FileName ?? $"photo-{photo.Id}");

        // Inline view; discourage caching of a sensitive image.
        Response.Headers["Cache-Control"] = "no-store";
        return File(data.Content, data.ContentType);
    }

    /// <summary>Returns (canView, canDownload) for the given viewer against a photo.</summary>
    private async Task<(bool canView, bool canDownload)> EvaluateAccessAsync(ProfilePhoto photo, ApplicationUser viewer, bool isAdmin)
    {
        bool isWomansPhoto = photo.Profile.Gender == Gender.Female;

        if (isAdmin)
        {
            if (isWomansPhoto)
            {
                // Women's (bride) photos: ONLY a female admin may view/download.
                bool femaleAdmin = viewer.Gender == Gender.Female;
                return (femaleAdmin, femaleAdmin);
            }
            // Men's (groom) photos: any admin may view/download.
            return (true, true);
        }

        // Owner: can always view their own photos.
        if (photo.Profile.UserId == viewer.Id) return (true, false);

        // Reveal flow currently applies to women's photos shown to a matched groom.
        if (!isWomansPhoto) return (false, false);

        // The bride's own visibility setting is the upper bound — Hidden/AdminOnly always blocks.
        if (photo.Visibility == PhotoVisibility.Hidden || photo.Visibility == PhotoVisibility.AdminOnly)
            return (false, false);

        // The groom may VIEW the bride's photo only after a FEMALE admin revealed it for their match.
        var viewerProfileId = await _db.Profiles
            .Where(p => p.UserId == viewer.Id)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync();

        if (viewerProfileId is null) return (false, false);

        bool revealed = await _db.Matches.AnyAsync(m =>
            m.FemaleProfileId == photo.ProfileId &&
            m.MaleProfileId == viewerProfileId &&
            m.PhotosRevealedToGroom);

        // Groom gets VIEW only; download stays for the female admin.
        return (revealed, false);
    }

    private async Task LogAsync(int photoId, string userId, PhotoAccessAction action)
    {
        _db.PhotoAccessLogs.Add(new PhotoAccessLog
        {
            PhotoId = photoId,
            AccessedByUserId = userId,
            Action = action,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            AccessedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
