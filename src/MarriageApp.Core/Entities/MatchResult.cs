namespace MarriageApp.Core.Entities;

/// <summary>
/// A cached snapshot of one system-computed candidate for a subject profile.
/// The admin's "Top 5" view reads these. Recomputed on demand; kept for audit so
/// we can see what the algorithm suggested at a point in time. NOT visible to users.
/// </summary>
public class MatchResult
{
    public int Id { get; set; }

    public int SubjectProfileId { get; set; }
    public Profile SubjectProfile { get; set; } = default!;

    public int CandidateProfileId { get; set; }
    public Profile CandidateProfile { get; set; } = default!;

    public decimal Score { get; set; }
    public string? BreakdownJson { get; set; }
    public int Rank { get; set; }

    public DateTime ComputedAt { get; set; }
}
