using MarriageApp.Core.Entities;
using MarriageApp.Core.Matching;

namespace MarriageApp.Web.ViewModels;

/// <summary>Admin's match-review screen for one subject profile.</summary>
public class MatchReviewViewModel
{
    public Profile Subject { get; set; } = default!;

    /// <summary>System-computed top candidates (admin-only), each with its score breakdown.</summary>
    public IReadOnlyList<MatchScore> TopMatches { get; set; } = new List<MatchScore>();

    /// <summary>Candidate profiles keyed by id, for rendering names/details next to scores.</summary>
    public Dictionary<int, Profile> Candidates { get; set; } = new();

    /// <summary>Existing Match workflow rows involving the subject (to show current decisions).</summary>
    public List<Match> ExistingMatches { get; set; } = new();
}
