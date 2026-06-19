using System.ComponentModel.DataAnnotations;

namespace MarriageApp.Core.Enums;

// =====================================================================================
// MULTI-SELECT PREFERENCE FLAGS
// -------------------------------------------------------------------------------------
// A user can accept MORE THAN ONE value for a categorical preference (e.g. accept a
// bride who wears niqab OR abaya). We model this as [Flags] enums stored as a single
// int bitmask on MatchRequirements.
//
// Bit values MUST be powers of two and MUST mirror the single-value enum members
// (e.g. DressCodeFlags.Niqab corresponds to DressCode.Niqab). The helper extension
// ToFlag() in EnumExtensions converts a single enum value to its flag bit so the
// matching algorithm can test membership with a single bitwise AND:
//
//     bool accepted = (requirement.AcceptedDressCodes & candidate.DressCode.ToFlag()) != 0;
//
// A value of None (0) means "no preference" -> matches anything (full points).
// =====================================================================================

[Flags]
public enum EducationLevelFlags
{
    None = 0,
    [Display(Name = "أقل من الثانوية")] BelowSecondary = 1 << 0,
    [Display(Name = "ثانوي / دبلوم")] Secondary = 1 << 1,
    [Display(Name = "معهد / دبلوم عالي")] Diploma = 1 << 2,
    [Display(Name = "جامعي")] University = 1 << 3,
    [Display(Name = "دراسات عليا")] Postgraduate = 1 << 4
}

[Flags]
public enum ReligiousCommitmentFlags
{
    None = 0,
    [Display(Name = "عدم الاختلاط")] NoMixing = 1 << 0,
    [Display(Name = "سماع دروس")] ListensToLessons = 1 << 1,
    [Display(Name = "طلب علم")] SeeksKnowledge = 1 << 2,
    [Display(Name = "حفظ القرآن")] MemorizesQuran = 1 << 3
}

[Flags]
public enum DressCodeFlags
{
    None = 0,
    [Display(Name = "منتقبة")] Niqab = 1 << 0,
    [Display(Name = "عبايات")] Abaya = 1 << 1,
    [Display(Name = "جيب وفساتين")] SkirtsAndDresses = 1 << 2,
    [Display(Name = "بنطلونات")] Trousers = 1 << 3
}

[Flags]
public enum MaritalStatusFlags
{
    None = 0,
    [Display(Name = "أعزب/آنسة")] Single = 1 << 0,
    [Display(Name = "مطلق/مطلقة")] Divorced = 1 << 1,
    [Display(Name = "أرمل/أرملة")] Widowed = 1 << 2
}

[Flags]
public enum FamilyCommitmentLevelFlags
{
    None = 0,
    [Display(Name = "غير ملتزمة")] NotCommitted = 1 << 0,
    [Display(Name = "ملتزمة إلى حد ما")] SomewhatCommitted = 1 << 1,
    [Display(Name = "ملتزمة")] Committed = 1 << 2,
    [Display(Name = "ملتزمة جدًا")] VeryCommitted = 1 << 3
}
