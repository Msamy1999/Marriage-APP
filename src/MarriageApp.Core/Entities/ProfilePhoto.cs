using System.ComponentModel.DataAnnotations;
using MarriageApp.Core.Enums;

namespace MarriageApp.Core.Entities;

/// <summary>
/// الوصف الشكلي والصور — a profile photo, stored ENCRYPTED at rest. Bytes are never
/// served as static files; they are only streamed through the gated PhotoController,
/// which enforces <see cref="Visibility"/> and logs every access (<see cref="PhotoAccessLog"/>).
/// </summary>
public class ProfilePhoto
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public Profile Profile { get; set; } = default!;

    /// <summary>Path to the encrypted blob on disk/storage (filename is a random GUID, not guessable).</summary>
    [Required, StringLength(260)]
    public string EncryptedBlobPath { get; set; } = default!;

    [Required, StringLength(100)]
    public string ContentType { get; set; } = default!;

    [StringLength(260)]
    public string? OriginalFileName { get; set; }

    /// <summary>Owner's chosen access policy — acts as the UPPER BOUND on who can ever see it.</summary>
    public PhotoVisibility Visibility { get; set; } = PhotoVisibility.AdminOnly;

    public bool IsPrimary { get; set; }

    public DateTime UploadedAt { get; set; }

    public ICollection<PhotoAccessLog> AccessLogs { get; set; } = new List<PhotoAccessLog>();
}
