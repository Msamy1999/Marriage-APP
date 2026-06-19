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
///   • Admin  -> may view AND download any photo.
///   • A matched groom -> may VIEW (not download) the bride's photos only when an
///     admin-approved <see cref="Match"/> exists between them, and the owner's
///     <see cref="PhotoVisibility"/> permits it.
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
        var userId = _userManager.GetUserId(User)!;
        var isAdmin = User.IsInRole(AppRoles.Admin);

        var photo = await _db.ProfilePhotos
            .Include(p => p.Profile)
            .FirstOrDefaultAsync(p => p.Id == photoId);

        if (photo is null) return NotFound();

        var (allowedView, allowedDownload) = await EvaluateAccessAsync(photo, userId, isAdmin);
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
    private async Task<(bool canView, bool canDownload)> EvaluateAccessAsync(ProfilePhoto photo, string viewerUserId, bool isAdmin)
    {
        // Admin: full access regardless of visibility.
        if (isAdmin) return (true, true);

        // Owner: can always view their own photos (no download needed here).
        if (photo.Profile.UserId == viewerUserId) return (true, false);

        // Owner's visibility setting is the upper bound for anyone else.
        if (photo.Visibility == PhotoVisibility.Hidden || photo.Visibility == PhotoVisibility.AdminOnly)
            return (false, false);

        // AfterMatchApproval: a non-admin may view the bride's photo only if an
        // approved match links the viewer (the groom) to the photo owner (the bride).
        var viewerProfileId = await _db.Profiles
            .Where(p => p.UserId == viewerUserId)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync();

        if (viewerProfileId is null) return (false, false);

        bool approvedMatchExists = await _db.Matches.AnyAsync(m =>
            m.Status >= MatchStatus.Approved &&
            m.FemaleProfileId == photo.ProfileId &&
            m.MaleProfileId == viewerProfileId);

        // Groom gets VIEW only; download stays admin-only.
        return (approvedMatchExists, false);
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
