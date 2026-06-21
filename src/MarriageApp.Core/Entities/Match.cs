using MarriageApp.Core.Enums;

namespace MarriageApp.Core.Entities;

/// <summary>
/// A match between a male and female profile that the ADMIN is working through.
/// Created when an admin shortlists a system-suggested pair, then advanced through
/// the <see cref="MatchStatus"/> workflow. Photo viewing for the groom unlocks at
/// <see cref="MatchStatus.Approved"/>.
/// </summary>
public class Match
{
    public int Id { get; set; }

    public int MaleProfileId { get; set; }
    public Profile MaleProfile { get; set; } = default!;

    public int FemaleProfileId { get; set; }
    public Profile FemaleProfile { get; set; } = default!;

    /// <summary>Compatibility percentage (0–100) at the time of shortlisting.</summary>
    public decimal Score { get; set; }

    /// <summary>JSON breakdown of per-criterion contributions, shown to the admin.</summary>
    public string? ScoreBreakdownJson { get; set; }

    public MatchStatus Status { get; set; } = MatchStatus.Pending;

    /// <summary>Identity user id of the admin who made the latest decision.</summary>
    public string? AdminUserId { get; set; }

    // ---- Photo reveal (women's photos -> groom) ----
    /// <summary>
    /// True once a FEMALE admin has explicitly revealed the bride's photos to this groom.
    /// The groom can only view the bride's photos when this is set (and the bride's own
    /// visibility setting still permits it).
    /// </summary>
    public bool PhotosRevealedToGroom { get; set; }
    public DateTime? PhotosRevealedAt { get; set; }
    /// <summary>Identity user id of the female admin who performed the reveal.</summary>
    public string? PhotosRevealedByAdminId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? DecisionAt { get; set; }
}
