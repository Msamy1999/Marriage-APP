using System.ComponentModel.DataAnnotations;
using MarriageApp.Core.Enums;

namespace MarriageApp.Core.Entities;

/// <summary>An outbound notification record (also serves as the in-app inbox).</summary>
public class Notification
{
    public int Id { get; set; }

    /// <summary>Identity user id of the recipient.</summary>
    [Required]
    public string UserId { get; set; } = default!;

    public NotificationChannel Channel { get; set; }

    [Required, StringLength(150)]
    public string Title { get; set; } = default!;

    [Required, StringLength(2000)]
    public string Message { get; set; } = default!;

    public bool IsRead { get; set; }

    /// <summary>Delivery state for external channels ("Sent", "Failed", "Skipped (no config)").</summary>
    [StringLength(50)]
    public string DeliveryStatus { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
}
