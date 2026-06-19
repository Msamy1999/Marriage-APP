using System.ComponentModel.DataAnnotations;
using MarriageApp.Core.Enums;

namespace MarriageApp.Core.Entities;

/// <summary>
/// Unified applicant profile for both grooms (العريس) and brides (العروسة).
/// Shared fields live here; gender-specific fields are nullable and only filled
/// for the relevant gender. The <see cref="Gender"/> column acts as the discriminator.
/// </summary>
public class Profile
{
    public int Id { get; set; }

    /// <summary>FK to the ASP.NET Identity user (defined in Infrastructure). One user = one profile.</summary>
    public string UserId { get; set; } = default!;

    public Gender Gender { get; set; }

    // ---- Shared core fields (both forms) ----
    [Required, StringLength(150)]
    public string Name { get; set; } = default!;                 // الاسم

    public DateTime DateOfBirth { get; set; }                    // تاريخ الميلاد

    /// <summary>العمر — persisted for fast range filtering; recomputed on save from DateOfBirth.</summary>
    public int Age { get; set; }

    [Range(120, 230)]
    public int HeightCm { get; set; }                            // الطول (سم)

    public EducationLevel EducationLevel { get; set; }           // المؤهل

    [StringLength(150)]
    public string? Occupation { get; set; }                      // العمل

    public MaritalStatus MaritalStatus { get; set; }             // الحالة الاجتماعية

    [Required, StringLength(100)]
    public string CurrentResidence { get; set; } = default!;     // محل السكن الحالي

    [Required, Phone, StringLength(30)]
    public string PhoneNumber { get; set; } = default!;          // رقم التليفون

    public ReligiousCommitment ReligiousCommitment { get; set; } // مستوى الالتزام

    [StringLength(2000)]
    public string? PersonalityDescription { get; set; }          // وصف الشخصية

    public bool HasHealthCondition { get; set; }                 // أمراض صحية/نفسية؟
    [StringLength(1000)]
    public string? HealthConditionDescription { get; set; }      // وصف الحالة الصحية

    public ProfileStatus Status { get; set; } = ProfileStatus.Incomplete;

    // ---- Female-only fields (العروسة) — null for males ----
    public DressCode? DressCode { get; set; }                    // شكل الملابس
    public bool? AcceptsTravel { get; set; }                     // هل توافقين على السفر؟
    public bool? AllowsOnlineViewing { get; set; }               // هل متاح رؤية أونلاين؟
    public bool? AcceptsDivorcedOrWithChildrenGroom { get; set; }// تقبلين عريس منفصل/لديه أطفال؟

    // ---- Male-only fields (العريس) — null for females ----
    [Range(40, 250)]
    public int? WeightKg { get; set; }                           // الوزن (كجم)
    [StringLength(100)]
    public string? FutureMaritalResidence { get; set; }          // محل سكن الزوجية
    public bool? IntendsToTravel { get; set; }                   // نية للسفر
    public bool? AcceptsDivorcedOrWithChildrenBride { get; set; }// قبول عروسة منفصلة/لديها أطفال؟

    // ---- Navigation ----
    public FamilyDetails? FamilyDetails { get; set; }
    public MatchRequirements? Requirements { get; set; }
    public ICollection<ProfilePhoto> Photos { get; set; } = new List<ProfilePhoto>();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Recomputes <see cref="Age"/> from <see cref="DateOfBirth"/> relative to a reference date.</summary>
    public void RecalculateAge(DateTime asOf)
    {
        var age = asOf.Year - DateOfBirth.Year;
        if (DateOfBirth.Date > asOf.AddYears(-age)) age--;
        Age = age;
    }
}
