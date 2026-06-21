using System.Text.Json;
using MarriageApp.Core.Entities;
using MarriageApp.Core.Enums;
using MarriageApp.Core.Services;
using MarriageApp.Infrastructure.Data;
using MarriageApp.Infrastructure.Identity;
using MarriageApp.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarriageApp.Web.Controllers;

/// <summary>
/// Admin portal: review applications, view system-generated Top-5 matches (with score
/// breakdowns), and drive the match decision workflow. Match scores are visible ONLY here.
/// </summary>
[Authorize(Roles = AppRoles.Admin)]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly IMatchingService _matching;
    private readonly INotificationService _notifications;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(AppDbContext db, IMatchingService matching,
        INotificationService notifications, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _matching = matching;
        _notifications = notifications;
        _userManager = userManager;
    }

    // ---- Applications list ----
    [HttpGet]
    public async Task<IActionResult> Index(Gender? gender, ProfileStatus? status)
    {
        var query = _db.Profiles.AsNoTracking().AsQueryable();
        if (gender is not null) query = query.Where(p => p.Gender == gender);
        if (status is not null) query = query.Where(p => p.Status == status);

        var profiles = await query.OrderByDescending(p => p.UpdatedAt).ToListAsync();
        return View(profiles);
    }

    // ---- Match review for one subject (computes + stores Top 5) ----
    [HttpGet]
    public async Task<IActionResult> MatchReview(int id)
    {
        var subject = await _db.Profiles
            .Include(p => p.FamilyDetails)
            .Include(p => p.Requirements).ThenInclude(r => r!.AcceptedResidences)
            .Include(p => p.Photos)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (subject is null) return NotFound();

        var top = await _matching.ComputeAndStoreTopMatchesAsync(id, take: 5);

        // Load candidate profiles (with photos) for display.
        var candidateIds = top.Select(t => t.CandidateProfileId).ToList();
        var candidates = await _db.Profiles
            .Include(p => p.Photos)
            .Include(p => p.FamilyDetails)
            .Where(p => candidateIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var existing = await _db.Matches
            .Where(m => m.MaleProfileId == id || m.FemaleProfileId == id)
            .ToListAsync();

        var currentAdmin = await _userManager.GetUserAsync(User);

        return View(new MatchReviewViewModel
        {
            Subject = subject,
            TopMatches = top,
            Candidates = candidates,
            ExistingMatches = existing,
            CurrentAdminIsFemale = currentAdmin?.Gender == Gender.Female
        });
    }

    // ---- Reveal the bride's photos to the matched groom (FEMALE admins only) ----
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RevealPhotos(int subjectProfileId, int candidateProfileId)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin?.Gender != Gender.Female)
        {
            TempData["Error"] = "كشف صور العروسة للعريس متاح للمشرفة فقط.";
            return RedirectToAction(nameof(MatchReview), new { id = subjectProfileId });
        }

        var subject = await _db.Profiles.FindAsync(subjectProfileId);
        var candidate = await _db.Profiles.FindAsync(candidateProfileId);
        if (subject is null || candidate is null) return NotFound();

        var (maleId, femaleId) = subject.Gender == Gender.Male
            ? (subjectProfileId, candidateProfileId)
            : (candidateProfileId, subjectProfileId);

        var match = await _db.Matches
            .FirstOrDefaultAsync(m => m.MaleProfileId == maleId && m.FemaleProfileId == femaleId);

        if (match is null)
        {
            match = new Match { MaleProfileId = maleId, FemaleProfileId = femaleId, CreatedAt = DateTime.UtcNow };
            _db.Matches.Add(match);
        }

        match.PhotosRevealedToGroom = true;
        match.PhotosRevealedAt = DateTime.UtcNow;
        match.PhotosRevealedByAdminId = admin.Id;
        await _db.SaveChangesAsync();

        // Let the groom know photos are now available to view.
        await NotifyPartyAsync(maleId);

        TempData["Message"] = "تم كشف صور العروسة للعريس بنجاح.";
        return RedirectToAction(nameof(MatchReview), new { id = subjectProfileId });
    }

    // ---- Create/advance a match decision ----
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(int subjectProfileId, int candidateProfileId, MatchStatus decision, double score)
    {
        var subject = await _db.Profiles.FindAsync(subjectProfileId);
        var candidate = await _db.Profiles.FindAsync(candidateProfileId);
        if (subject is null || candidate is null) return NotFound();

        // Normalize to (male, female) so the unique index holds regardless of who is the subject.
        var (maleId, femaleId) = subject.Gender == Gender.Male
            ? (subjectProfileId, candidateProfileId)
            : (candidateProfileId, subjectProfileId);

        var match = await _db.Matches
            .FirstOrDefaultAsync(m => m.MaleProfileId == maleId && m.FemaleProfileId == femaleId);

        if (match is null)
        {
            match = new Match
            {
                MaleProfileId = maleId,
                FemaleProfileId = femaleId,
                Score = (decimal)Math.Round(score, 2),
                CreatedAt = DateTime.UtcNow
            };
            _db.Matches.Add(match);
        }

        match.Status = decision;
        match.AdminUserId = _userManager.GetUserId(User);
        match.DecisionAt = DateTime.UtcNow;

        // Update both parties' profile status + notify them at meaningful milestones.
        if (decision is MatchStatus.Approved or MatchStatus.Accepted)
        {
            subject.Status = ProfileStatus.Matched;
            candidate.Status = ProfileStatus.Matched;
        }

        await _db.SaveChangesAsync();

        if (decision is MatchStatus.Approved or MatchStatus.Contacted or MatchStatus.Accepted)
        {
            await NotifyPartyAsync(maleId);
            await NotifyPartyAsync(femaleId);
        }

        TempData["Message"] = "تم تحديث حالة التطابق بنجاح.";
        return RedirectToAction(nameof(MatchReview), new { id = subjectProfileId });
    }

    /// <summary>Sends the "match found" notification to a profile's owner across configured channels.</summary>
    private async Task NotifyPartyAsync(int profileId)
    {
        var profile = await _db.Profiles.FindAsync(profileId);
        if (profile is null) return;

        var user = await _userManager.FindByIdAsync(profile.UserId);
        if (user is null) return;

        await _notifications.NotifyAsync(new NotificationRequest
        {
            UserId = user.Id,
            Email = user.Email,
            PhoneNumber = profile.PhoneNumber,
            Title = "بشرى سارة من منصة سَكينة",
            Message = "تم إيجاد طرف مناسب لك! فضلاً سجّل الدخول لمتابعة التفاصيل، وسيتواصل معك المشرف قريباً.",
            Channels = new[] { NotificationChannel.InApp, NotificationChannel.Email, NotificationChannel.Sms, NotificationChannel.WhatsApp }
        });
    }

    // ---- Photo access audit log (trust/transparency) ----
    [HttpGet]
    public async Task<IActionResult> PhotoAudit(int profileId)
    {
        var logs = await _db.PhotoAccessLogs
            .Include(l => l.Photo)
            .Where(l => l.Photo.ProfileId == profileId)
            .OrderByDescending(l => l.AccessedAt)
            .ToListAsync();
        ViewBag.ProfileId = profileId;
        return View(logs);
    }
}
