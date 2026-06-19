using System.ComponentModel.DataAnnotations;
using MarriageApp.Core.Enums;

namespace MarriageApp.Core.Entities;

/// <summary>بيانات العائلة — one-to-one with <see cref="Profile"/>.</summary>
public class FamilyDetails
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public Profile Profile { get; set; } = default!;

    [StringLength(150)]
    public string? FatherOccupation { get; set; }               // عمل الأب

    [StringLength(150)]
    public string? MotherOccupationOrEducation { get; set; }    // عمل الأم / مؤهل الأم

    [StringLength(500)]
    public string? SiblingsCountAndStudies { get; set; }        // عدد الإخوة ودراستهم

    public FamilyCommitmentLevel FamilyCommitmentLevel { get; set; } // مدى التزام الأسرة

    /// <summary>رقم الأم أو الأخت — collected on the groom's form for the bride's family to contact.</summary>
    [Phone, StringLength(30)]
    public string? MotherOrSisterPhone { get; set; }
}
