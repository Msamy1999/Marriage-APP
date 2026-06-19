using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace MarriageApp.Core.Enums;

/// <summary>
/// Helpers to (a) convert a single-value enum to its matching [Flags] bit so the
/// matching algorithm can test set membership, and (b) read the Arabic [Display] name
/// for any enum value so views render RTL labels without a giant switch.
/// </summary>
public static class EnumExtensions
{
    // ---- single-value -> flag-bit mappings (kept explicit so a refactor breaks loudly) ----

    public static EducationLevelFlags ToFlag(this EducationLevel value) => value switch
    {
        EducationLevel.BelowSecondary => EducationLevelFlags.BelowSecondary,
        EducationLevel.Secondary => EducationLevelFlags.Secondary,
        EducationLevel.Diploma => EducationLevelFlags.Diploma,
        EducationLevel.University => EducationLevelFlags.University,
        EducationLevel.Postgraduate => EducationLevelFlags.Postgraduate,
        _ => EducationLevelFlags.None
    };

    public static ReligiousCommitmentFlags ToFlag(this ReligiousCommitment value) => value switch
    {
        ReligiousCommitment.NoMixing => ReligiousCommitmentFlags.NoMixing,
        ReligiousCommitment.ListensToLessons => ReligiousCommitmentFlags.ListensToLessons,
        ReligiousCommitment.SeeksKnowledge => ReligiousCommitmentFlags.SeeksKnowledge,
        ReligiousCommitment.MemorizesQuran => ReligiousCommitmentFlags.MemorizesQuran,
        _ => ReligiousCommitmentFlags.None
    };

    public static DressCodeFlags ToFlag(this DressCode value) => value switch
    {
        DressCode.Niqab => DressCodeFlags.Niqab,
        DressCode.Abaya => DressCodeFlags.Abaya,
        DressCode.SkirtsAndDresses => DressCodeFlags.SkirtsAndDresses,
        DressCode.Trousers => DressCodeFlags.Trousers,
        _ => DressCodeFlags.None
    };

    public static MaritalStatusFlags ToFlag(this MaritalStatus value) => value switch
    {
        MaritalStatus.Single => MaritalStatusFlags.Single,
        MaritalStatus.Divorced => MaritalStatusFlags.Divorced,
        MaritalStatus.Widowed => MaritalStatusFlags.Widowed,
        _ => MaritalStatusFlags.None
    };

    public static FamilyCommitmentLevelFlags ToFlag(this FamilyCommitmentLevel value) => value switch
    {
        FamilyCommitmentLevel.NotCommitted => FamilyCommitmentLevelFlags.NotCommitted,
        FamilyCommitmentLevel.SomewhatCommitted => FamilyCommitmentLevelFlags.SomewhatCommitted,
        FamilyCommitmentLevel.Committed => FamilyCommitmentLevelFlags.Committed,
        FamilyCommitmentLevel.VeryCommitted => FamilyCommitmentLevelFlags.VeryCommitted,
        _ => FamilyCommitmentLevelFlags.None
    };

    /// <summary>Reads the Arabic [Display(Name=...)] text for any enum value (falls back to the member name).</summary>
    public static string GetDisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        var display = member?.GetCustomAttribute<DisplayAttribute>();
        return display?.Name ?? value.ToString();
    }
}
