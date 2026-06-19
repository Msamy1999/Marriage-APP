using System.ComponentModel.DataAnnotations;
using MarriageApp.Core.Enums;

namespace MarriageApp.Web.ViewModels;

/// <summary>
/// Unified create/edit form for both grooms and brides. The view shows gender-specific
/// sections based on <see cref="Gender"/>. Multi-select preferences are bound as lists of
/// enum values (checkbox groups) and combined into [Flags] bitmasks by the controller.
/// </summary>
public class ProfileFormViewModel
{
    public int? ProfileId { get; set; }
    public Gender Gender { get; set; }

    // ---- Core ----
    [Required(ErrorMessage = "الاسم مطلوب"), Display(Name = "الاسم")]
    public string Name { get; set; } = default!;

    [Required, DataType(DataType.Date), Display(Name = "تاريخ الميلاد")]
    public DateTime DateOfBirth { get; set; } = new DateTime(1995, 1, 1);

    [Range(120, 230, ErrorMessage = "الطول يجب أن يكون بين 120 و230 سم"), Display(Name = "الطول (سم)")]
    public int HeightCm { get; set; } = 170;

    [Display(Name = "المؤهل")]
    public EducationLevel EducationLevel { get; set; }

    [Display(Name = "العمل")]
    public string? Occupation { get; set; }

    [Display(Name = "الحالة الاجتماعية")]
    public MaritalStatus MaritalStatus { get; set; }

    [Required(ErrorMessage = "محل السكن مطلوب"), Display(Name = "محل السكن الحالي")]
    public string CurrentResidence { get; set; } = default!;

    [Required(ErrorMessage = "رقم الهاتف مطلوب"), Phone, Display(Name = "رقم الهاتف")]
    public string PhoneNumber { get; set; } = default!;

    [Display(Name = "مستوى الالتزام")]
    public ReligiousCommitment ReligiousCommitment { get; set; }

    [Display(Name = "وصف الشخصية")]
    public string? PersonalityDescription { get; set; }

    [Display(Name = "هل توجد أمراض صحية أو نفسية؟")]
    public bool HasHealthCondition { get; set; }

    [Display(Name = "تفاصيل الحالة الصحية")]
    public string? HealthConditionDescription { get; set; }

    // ---- Female-only ----
    [Display(Name = "شكل الملابس")]
    public DressCode? DressCode { get; set; }
    [Display(Name = "هل توافقين على السفر؟")]
    public bool? AcceptsTravel { get; set; }
    [Display(Name = "هل متاح رؤية أونلاين؟")]
    public bool? AllowsOnlineViewing { get; set; }
    [Display(Name = "هل تقبلين بعريس منفصل أو لديه أطفال؟")]
    public bool? AcceptsDivorcedOrWithChildrenGroom { get; set; }

    // ---- Male-only ----
    [Display(Name = "الوزن (كجم)")]
    public int? WeightKg { get; set; }
    [Display(Name = "محل سكن الزوجية")]
    public string? FutureMaritalResidence { get; set; }
    [Display(Name = "هل لديك نية للسفر؟")]
    public bool? IntendsToTravel { get; set; }
    [Display(Name = "هل تقبل بعروسة منفصلة أو لديها أطفال؟")]
    public bool? AcceptsDivorcedOrWithChildrenBride { get; set; }

    // ---- Family ----
    [Display(Name = "عمل الأب")] public string? FatherOccupation { get; set; }
    [Display(Name = "عمل/مؤهل الأم")] public string? MotherOccupationOrEducation { get; set; }
    [Display(Name = "عدد الإخوة ودراستهم")] public string? SiblingsCountAndStudies { get; set; }
    [Display(Name = "مدى التزام الأسرة")] public FamilyCommitmentLevel FamilyCommitmentLevel { get; set; }
    [Display(Name = "رقم الأم أو الأخت")] public string? MotherOrSisterPhone { get; set; }

    // ---- Requirements: ranges ----
    [Display(Name = "أقل عمر مطلوب")] public int? MinAge { get; set; }
    [Display(Name = "أكبر عمر مطلوب")] public int? MaxAge { get; set; }
    [Display(Name = "أقل طول مطلوب (سم)")] public int? MinHeightCm { get; set; }
    [Display(Name = "أكبر طول مطلوب (سم)")] public int? MaxHeightCm { get; set; }

    // ---- Requirements: multi-select sets (checkbox groups) ----
    [Display(Name = "المؤهلات المقبولة")]
    public List<EducationLevel> AcceptedEducationLevels { get; set; } = new();
    [Display(Name = "مستويات الالتزام المقبولة")]
    public List<ReligiousCommitment> AcceptedReligiousCommitments { get; set; } = new();
    [Display(Name = "أشكال الملابس المقبولة (للعروسة)")]
    public List<DressCode> AcceptedDressCodes { get; set; } = new();
    [Display(Name = "الحالات الاجتماعية المقبولة")]
    public List<MaritalStatus> AcceptedMaritalStatuses { get; set; } = new();
    [Display(Name = "مستويات التزام الأسرة المقبولة")]
    public List<FamilyCommitmentLevel> AcceptedFamilyCommitmentLevels { get; set; } = new();
    [Display(Name = "مدن السكن المقبولة (افصل بينها بفاصلة)")]
    public string? AcceptedResidencesCsv { get; set; }

    // ---- Requirements: booleans + free text ----
    [Display(Name = "يشترط الاستعداد/النية للسفر")] public bool RequiresTravelWillingness { get; set; }
    [Display(Name = "يقبل طرفًا سبق له الزواج")] public bool AcceptsDivorced { get; set; }
    [Display(Name = "يقبل طرفًا لديه أطفال")] public bool AcceptsWithChildren { get; set; }
    [Display(Name = "اشتراطات أخرى")] public string? OtherConditions { get; set; }
}
