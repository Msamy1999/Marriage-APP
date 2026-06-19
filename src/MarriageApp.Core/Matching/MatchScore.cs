namespace MarriageApp.Core.Matching;

/// <summary>A single criterion's contribution to the overall compatibility score.</summary>
public class CriterionScore
{
    public string Criterion { get; set; } = default!;   // e.g. "العمر"
    public double Weight { get; set; }                   // max points this criterion can contribute
    public double Earned { get; set; }                   // points actually earned (0..Weight)
    public string Detail { get; set; } = default!;       // human-readable explanation (Arabic)
}

/// <summary>
/// The result of scoring one candidate against a subject profile. Returned to the
/// ADMIN only — users never see scores or other profiles.
/// </summary>
public class MatchScore
{
    public int SubjectProfileId { get; set; }
    public int CandidateProfileId { get; set; }
    public string CandidateName { get; set; } = default!;

    /// <summary>Final mutual compatibility percentage (0–100), already penalty-adjusted.</summary>
    public double Percentage { get; set; }

    /// <summary>Subject -> candidate raw score (0–100) before mutual averaging.</summary>
    public double SubjectToCandidate { get; set; }
    /// <summary>Candidate -> subject raw score (0–100) before mutual averaging.</summary>
    public double CandidateToSubject { get; set; }

    /// <summary>Per-criterion breakdown of the subject -> candidate evaluation (for admin display).</summary>
    public List<CriterionScore> Breakdown { get; set; } = new();

    /// <summary>Hard constraints that were violated (e.g. "غير مستعد للسفر"). Drives the penalty.</summary>
    public List<string> ViolatedConstraints { get; set; } = new();
}
