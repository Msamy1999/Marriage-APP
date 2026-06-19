using System.ComponentModel.DataAnnotations;
using MarriageApp.Core.Enums;

namespace MarriageApp.Core.Entities;

/// <summary>
/// Audit record of every photo access attempt (view / download / denied). Critical for
/// trust in this domain — proves who looked at a bride's photo and when.
/// </summary>
public class PhotoAccessLog
{
    public int Id { get; set; }

    public int PhotoId { get; set; }
    public ProfilePhoto Photo { get; set; } = default!;

    /// <summary>Identity user id of whoever attempted access.</summary>
    [Required]
    public string AccessedByUserId { get; set; } = default!;

    public PhotoAccessAction Action { get; set; }

    [StringLength(64)]
    public string? IpAddress { get; set; }

    public DateTime AccessedAt { get; set; }
}
