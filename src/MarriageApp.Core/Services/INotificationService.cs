using MarriageApp.Core.Enums;

namespace MarriageApp.Core.Services;

/// <summary>A request to notify a single user across one or more channels.</summary>
public class NotificationRequest
{
    public string UserId { get; set; } = default!;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;

    /// <summary>Channels to attempt. Defaults to in-app + email.</summary>
    public IReadOnlyCollection<NotificationChannel> Channels { get; set; } =
        new[] { NotificationChannel.InApp, NotificationChannel.Email };
}

/// <summary>Orchestrates delivery of a notification across the configured channels.</summary>
public interface INotificationService
{
    Task NotifyAsync(NotificationRequest request, CancellationToken ct = default);
}

/// <summary>
/// One delivery channel (in-app, email, SMS, WhatsApp). Implementations that need
/// external provider config no-op (and report "Skipped") when keys are absent.
/// </summary>
public interface INotificationChannelSender
{
    NotificationChannel Channel { get; }

    /// <summary>True when this channel is configured and ready to send.</summary>
    bool IsEnabled { get; }

    /// <summary>Attempts delivery; returns a short status string for the audit record.</summary>
    Task<string> SendAsync(NotificationRequest request, CancellationToken ct = default);
}
