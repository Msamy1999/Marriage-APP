using MarriageApp.Core.Matching;

namespace MarriageApp.Core.Services;

/// <summary>
/// Computes compatibility between opposite-gender profiles and returns the top
/// candidates for a subject. Results are ADMIN-ONLY by design.
/// </summary>
public interface IMatchingService
{
    /// <summary>
    /// Returns the top <paramref name="take"/> opposite-gender candidates for the
    /// given profile, sorted by descending compatibility percentage.
    /// </summary>
    Task<IReadOnlyList<MatchScore>> GetTopMatchesAsync(int profileId, int take = 5, CancellationToken ct = default);

    /// <summary>Computes and persists the top matches as <c>MatchResult</c> rows for audit/caching.</summary>
    Task<IReadOnlyList<MatchScore>> ComputeAndStoreTopMatchesAsync(int profileId, int take = 5, CancellationToken ct = default);
}
