using System.Text.Json;
using MarriageApp.Core.Entities;
using MarriageApp.Core.Enums;
using MarriageApp.Core.Matching;
using MarriageApp.Core.Services;
using MarriageApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MarriageApp.Infrastructure.Services;

/// <summary>
/// Compatibility engine. For a subject profile it scores every opposite-gender,
/// active candidate and returns the best matches. Scoring is:
///
///   1. WEIGHTED:   each criterion contributes up to a configurable max (see MatchingWeights).
///   2. RANGE-BASED: age/height earn full points inside the requested band and decay
///                   linearly just outside it (so a near-miss still ranks).
///   3. SET-BASED:   categorical preferences are MULTI-SELECT [Flags] sets; the candidate
///                   earns the points if its single value is in the accepted set. An empty
///                   set means "no preference" -> full points (never penalizes).
///   4. SOFT HARD-CONSTRAINTS: boolean must-haves (travel, accepts-divorced) apply a heavy
///                   penalty multiplier instead of excluding, so a small launch pool never
///                   returns zero matches. Can be flipped to a strict filter via config.
///   5. MUTUAL:      the final percentage averages subject->candidate and candidate->subject,
///                   so a high score requires BOTH sides' requirements to be reasonably met.
///
/// Results are ADMIN-ONLY by design — never surfaced to end users.
/// </summary>
public class MatchingService : IMatchingService
{
    private readonly AppDbContext _db;
    private readonly MatchingWeights _w;

    public MatchingService(AppDbContext db, IOptions<MatchingWeights> weights)
    {
        _db = db;
        _w = weights.Value;
    }

    public async Task<IReadOnlyList<MatchScore>> GetTopMatchesAsync(int profileId, int take = 5, CancellationToken ct = default)
    {
        var subject = await LoadFullProfileAsync(profileId, ct)
            ?? throw new InvalidOperationException($"Profile {profileId} not found.");

        var oppositeGender = subject.Gender == Gender.Male ? Gender.Female : Gender.Male;

        // Pull all eligible candidates (opposite gender, actively seeking). The DB indexes
        // on (Gender, Status) keep this cheap; scoring itself runs in memory.
        var candidates = await _db.Profiles
            .Include(p => p.Requirements).ThenInclude(r => r!.AcceptedResidences)
            .Include(p => p.FamilyDetails)
            .Where(p => p.Gender == oppositeGender
                        && p.Id != profileId
                        && (p.Status == ProfileStatus.PendingMatch || p.Status == ProfileStatus.Matched))
            .AsNoTracking()
            .ToListAsync(ct);

        var scores = new List<MatchScore>(candidates.Count);
        foreach (var candidate in candidates)
        {
            var score = ScorePair(subject, candidate);
            if (score is not null) scores.Add(score);
        }

        return scores
            .OrderByDescending(s => s.Percentage)
            .Take(take)
            .ToList();
    }

    public async Task<IReadOnlyList<MatchScore>> ComputeAndStoreTopMatchesAsync(int profileId, int take = 5, CancellationToken ct = default)
    {
        var top = await GetTopMatchesAsync(profileId, take, ct);

        // Replace any previously cached results for this subject.
        var existing = _db.MatchResults.Where(m => m.SubjectProfileId == profileId);
        _db.MatchResults.RemoveRange(existing);

        var now = DateTime.UtcNow;
        var rank = 1;
        foreach (var s in top)
        {
            _db.MatchResults.Add(new MatchResult
            {
                SubjectProfileId = s.SubjectProfileId,
                CandidateProfileId = s.CandidateProfileId,
                Score = (decimal)Math.Round(s.Percentage, 2),
                BreakdownJson = JsonSerializer.Serialize(s.Breakdown),
                Rank = rank++,
                ComputedAt = now
            });
        }
        await _db.SaveChangesAsync(ct);
        return top;
    }

    private Task<Profile?> LoadFullProfileAsync(int id, CancellationToken ct) =>
        _db.Profiles
            .Include(p => p.Requirements).ThenInclude(r => r!.AcceptedResidences)
            .Include(p => p.FamilyDetails)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    // -------------------------------------------------------------------------------------
    // Pair scoring: evaluate both directions, apply penalties, average to a mutual percentage.
    // -------------------------------------------------------------------------------------
    private MatchScore? ScorePair(Profile subject, Profile candidate)
    {
        // Subject's requirements judging the candidate (this drives the admin-facing breakdown).
        var forward = ScoreDirection(subject, candidate);
        // Candidate's requirements judging the subject (so the match is mutually acceptable).
        var backward = ScoreDirection(candidate, subject);

        // Strict-filter mode: drop the candidate outright if either side has a violated must-have.
        if (_w.TreatConstraintsAsStrictFilter &&
            (forward.Violations.Count > 0 || backward.Violations.Count > 0))
        {
            return null;
        }

        // Soft mode: a violation heavily penalizes the offending direction's score.
        var forwardScore = ApplyPenalty(forward.Percentage, forward.Violations.Count);
        var backwardScore = ApplyPenalty(backward.Percentage, backward.Violations.Count);

        var mutual = (forwardScore + backwardScore) / 2.0;

        var (male, female) = subject.Gender == Gender.Male ? (subject, candidate) : (candidate, subject);
        _ = (male, female); // documented intent: scores are gender-agnostic and mutual.

        return new MatchScore
        {
            SubjectProfileId = subject.Id,
            CandidateProfileId = candidate.Id,
            CandidateName = candidate.Name,
            Percentage = Math.Round(mutual, 1),
            SubjectToCandidate = Math.Round(forwardScore, 1),
            CandidateToSubject = Math.Round(backwardScore, 1),
            Breakdown = forward.Breakdown,
            ViolatedConstraints = forward.Violations.Concat(backward.Violations).Distinct().ToList()
        };
    }

    private double ApplyPenalty(double percentage, int violationCount) =>
        violationCount == 0 ? percentage : percentage * _w.HardConstraintPenaltyFactor;

    private sealed record DirectionResult(double Percentage, List<CriterionScore> Breakdown, List<string> Violations);

    /// <summary>
    /// Scores how well <paramref name="target"/> satisfies <paramref name="evaluator"/>'s
    /// requirements. Returns a 0–100 percentage normalized over the criteria that apply,
    /// the per-criterion breakdown, and any violated boolean constraints.
    /// </summary>
    private DirectionResult ScoreDirection(Profile evaluator, Profile target)
    {
        var req = evaluator.Requirements;
        var breakdown = new List<CriterionScore>();
        var violations = new List<string>();

        // No requirements captured yet -> treat as fully open (neutral 100%).
        if (req is null)
        {
            breakdown.Add(new CriterionScore { Criterion = "بدون اشتراطات", Weight = 100, Earned = 100, Detail = "لم يحدد الطرف اشتراطات" });
            return new DirectionResult(100, breakdown, violations);
        }

        // ---- Age (range) ----
        AddRange(breakdown, "العمر", _w.Age, target.Age, req.MinAge, req.MaxAge, _w.AgeFalloffYears, "سنة");

        // ---- Height (range) ----
        AddRange(breakdown, "الطول", _w.Height, target.HeightCm, req.MinHeightCm, req.MaxHeightCm, _w.HeightFalloffCm, "سم");

        // ---- Religious commitment (ordered set, adjacency credit) ----
        AddOrderedSet(breakdown, "مستوى الالتزام", _w.ReligiousCommitment,
            (int)target.ReligiousCommitment, (int)req.AcceptedReligiousCommitments,
            target.ReligiousCommitment.GetDisplayName());

        // ---- Education (ordered set, adjacency credit) ----
        AddOrderedSet(breakdown, "المؤهل", _w.Education,
            (int)target.EducationLevel, (int)req.AcceptedEducationLevels,
            target.EducationLevel.GetDisplayName());

        // ---- Marital status (plain set) ----
        AddPlainSet(breakdown, "الحالة الاجتماعية", _w.MaritalStatus,
            (int)target.MaritalStatus.ToFlag(), (int)req.AcceptedMaritalStatuses,
            target.MaritalStatus.GetDisplayName());

        // ---- Dress code (plain set) — only meaningful when the target is the bride ----
        if (target.Gender == Gender.Female && target.DressCode.HasValue)
        {
            AddPlainSet(breakdown, "شكل الملابس", _w.DressCode,
                (int)target.DressCode.Value.ToFlag(), (int)req.AcceptedDressCodes,
                target.DressCode.Value.GetDisplayName());
        }

        // ---- Residence (city list) ----
        AddResidence(breakdown, "محل السكن", _w.Residence, target.CurrentResidence, req.AcceptedResidences);

        // ---- Family commitment (ordered set) ----
        if (target.FamilyDetails is not null)
        {
            AddOrderedSet(breakdown, "التزام الأسرة", _w.FamilyCommitment,
                (int)target.FamilyDetails.FamilyCommitmentLevel, (int)req.AcceptedFamilyCommitmentLevels,
                target.FamilyDetails.FamilyCommitmentLevel.GetDisplayName());
        }

        // ---- Boolean hard-ish constraints ----
        // Travel: if the evaluator requires it, the target must be willing/intend to travel.
        if (req.RequiresTravelWillingness)
        {
            bool willing = target.Gender == Gender.Female
                ? target.AcceptsTravel == true
                : target.IntendsToTravel == true;
            if (!willing) violations.Add("الطرف الآخر غير مستعد للسفر");
        }

        // Previously-married: if the target is divorced/widowed and the evaluator does not accept that.
        bool targetPreviouslyMarried = target.MaritalStatus is MaritalStatus.Divorced or MaritalStatus.Widowed;
        if (targetPreviouslyMarried && !req.AcceptsDivorced)
        {
            violations.Add("لا يقبل الطرف الآخر من سبق له الزواج");
        }

        // Normalize earned/possible over the criteria that actually applied.
        double possible = breakdown.Sum(c => c.Weight);
        double earned = breakdown.Sum(c => c.Earned);
        double percentage = possible <= 0 ? 100 : (earned / possible) * 100.0;

        return new DirectionResult(percentage, breakdown, violations);
    }

    // ---- Scoring primitives -------------------------------------------------------------

    /// <summary>Full points inside [min,max]; linear decay within <paramref name="falloff"/> beyond it; else 0.</summary>
    private void AddRange(List<CriterionScore> bd, string name, double weight,
        int value, int? min, int? max, int falloff, string unit)
    {
        if (weight <= 0) return;

        if (min is null && max is null)
        {
            bd.Add(new CriterionScore { Criterion = name, Weight = weight, Earned = weight, Detail = "بدون تفضيل" });
            return;
        }

        int lo = min ?? int.MinValue;
        int hi = max ?? int.MaxValue;

        double ratio;
        string detail;
        if (value >= lo && value <= hi)
        {
            ratio = 1.0;
            detail = $"{value} {unit} داخل النطاق المطلوب";
        }
        else
        {
            int distance = value < lo ? lo - value : value - hi;
            ratio = falloff > 0 ? Math.Max(0, 1.0 - (double)distance / falloff) : 0;
            detail = $"{value} {unit} خارج النطاق بفارق {distance} {unit}";
        }

        bd.Add(new CriterionScore { Criterion = name, Weight = weight, Earned = Math.Round(weight * ratio, 2), Detail = detail });
    }

    /// <summary>
    /// Ordered-enum set membership with adjacency credit. <paramref name="valueIndex"/> is the
    /// single-value enum int; <paramref name="acceptedMask"/> is the [Flags] bitmask. Full credit
    /// if the value's bit is set, partial credit if an immediately adjacent level is accepted.
    /// </summary>
    private void AddOrderedSet(List<CriterionScore> bd, string name, double weight,
        int valueIndex, int acceptedMask, string valueLabel)
    {
        if (weight <= 0) return;

        if (acceptedMask == 0)
        {
            bd.Add(new CriterionScore { Criterion = name, Weight = weight, Earned = weight, Detail = "بدون تفضيل" });
            return;
        }

        // The single-value enums are 1-based; their flag bit is 1 << (value-1).
        int valueBit = 1 << (valueIndex - 1);
        if ((acceptedMask & valueBit) != 0)
        {
            bd.Add(new CriterionScore { Criterion = name, Weight = weight, Earned = weight, Detail = $"{valueLabel} ضمن المقبول" });
            return;
        }

        int lowerBit = valueIndex - 2 >= 0 ? 1 << (valueIndex - 2) : 0;
        int upperBit = 1 << valueIndex;
        bool adjacent = (lowerBit != 0 && (acceptedMask & lowerBit) != 0) || (acceptedMask & upperBit) != 0;

        double ratio = adjacent ? _w.AdjacentEnumCredit : 0;
        string detail = adjacent ? $"{valueLabel} قريب من المقبول" : $"{valueLabel} غير مقبول";
        bd.Add(new CriterionScore { Criterion = name, Weight = weight, Earned = Math.Round(weight * ratio, 2), Detail = detail });
    }

    /// <summary>Plain (unordered) set membership: full credit if in set, 0 otherwise, full if no preference.</summary>
    private void AddPlainSet(List<CriterionScore> bd, string name, double weight,
        int valueBit, int acceptedMask, string valueLabel)
    {
        if (weight <= 0) return;

        if (acceptedMask == 0)
        {
            bd.Add(new CriterionScore { Criterion = name, Weight = weight, Earned = weight, Detail = "بدون تفضيل" });
            return;
        }

        bool inSet = (acceptedMask & valueBit) != 0;
        bd.Add(new CriterionScore
        {
            Criterion = name,
            Weight = weight,
            Earned = inSet ? weight : 0,
            Detail = inSet ? $"{valueLabel} ضمن المقبول" : $"{valueLabel} غير مقبول"
        });
    }

    /// <summary>City-list membership (case/space-insensitive). Empty list = no preference.</summary>
    private void AddResidence(List<CriterionScore> bd, string name, double weight,
        string targetCity, ICollection<RequirementResidence> accepted)
    {
        if (weight <= 0) return;

        if (accepted.Count == 0)
        {
            bd.Add(new CriterionScore { Criterion = name, Weight = weight, Earned = weight, Detail = "بدون تفضيل" });
            return;
        }

        bool match = accepted.Any(c => string.Equals(c.City.Trim(), targetCity.Trim(), StringComparison.OrdinalIgnoreCase));
        bd.Add(new CriterionScore
        {
            Criterion = name,
            Weight = weight,
            Earned = match ? weight : 0,
            Detail = match ? $"{targetCity} ضمن المدن المقبولة" : $"{targetCity} خارج المدن المقبولة"
        });
    }
}
