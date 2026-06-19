using System.ComponentModel.DataAnnotations;
using MarriageApp.Core.Enums;

namespace MarriageApp.Core.Entities;

/// <summary>
/// البيانات المطلوبة في الطرف الآخر — what this applicant wants in a partner.
/// Stored as numeric ranges + [Flags] multi-select sets so the matching algorithm
/// can score candidates efficiently. A null range bound or a "None" flag set means
/// "no preference" and never penalizes a candidate.
/// </summary>
public class MatchRequirements
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public Profile Profile { get; set; } = default!;

    // ---- Numeric ranges ----
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public int? MinHeightCm { get; set; }
    public int? MaxHeightCm { get; set; }

    // ---- Multi-select sets (checkbox UI; accept ANY of the chosen values) ----
    public EducationLevelFlags AcceptedEducationLevels { get; set; } = EducationLevelFlags.None;
    public ReligiousCommitmentFlags AcceptedReligiousCommitments { get; set; } = ReligiousCommitmentFlags.None;
    /// <summary>Dress codes the groom accepts in a bride (only meaningful groom -> bride).</summary>
    public DressCodeFlags AcceptedDressCodes { get; set; } = DressCodeFlags.None;
    public MaritalStatusFlags AcceptedMaritalStatuses { get; set; } = MaritalStatusFlags.None;
    public FamilyCommitmentLevelFlags AcceptedFamilyCommitmentLevels { get; set; } = FamilyCommitmentLevelFlags.None;

    /// <summary>
    /// Accepted residence cities. Open-ended, so stored as a child collection rather
    /// than a flags enum. Empty = no preference.
    /// </summary>
    public ICollection<RequirementResidence> AcceptedResidences { get; set; } = new List<RequirementResidence>();

    // ---- Boolean hard-ish constraints (soft-penalized by the algorithm, configurable to strict) ----
    public bool RequiresTravelWillingness { get; set; }  // يشترط الاستعداد/النية للسفر
    public bool AcceptsDivorced { get; set; }            // يقبل/تقبل طرفًا مطلقًا
    public bool AcceptsWithChildren { get; set; }        // يقبل/تقبل طرفًا لديه أطفال

    [StringLength(1000)]
    public string? OtherConditions { get; set; }         // اشتراطات معينة (نص حر)
}

/// <summary>A single accepted residence city for a <see cref="MatchRequirements"/> row.</summary>
public class RequirementResidence
{
    public int Id { get; set; }
    public int MatchRequirementsId { get; set; }
    public MatchRequirements MatchRequirements { get; set; } = default!;

    [Required, StringLength(100)]
    public string City { get; set; } = default!;
}
