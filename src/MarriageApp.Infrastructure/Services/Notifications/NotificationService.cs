using MarriageApp.Core.Services;
using Microsoft.Extensions.Logging;

namespace MarriageApp.Infrastructure.Services.Notifications;

/// <summary>
/// Fans a notification out to every requested channel that has a registered sender,
/// attempting each independently so one failure never blocks the others. The in-app
/// channel persists the inbox row; external channels (email/SMS/WhatsApp) report a
/// status and no-op cleanly when their provider isn't configured.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IEnumerable<INotificationChannelSender> _senders;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IEnumerable<INotificationChannelSender> senders, ILogger<NotificationService> logger)
    {
        _senders = senders;
        _logger = logger;
    }

    public async Task NotifyAsync(NotificationRequest request, CancellationToken ct = default)
    {
        foreach (var channel in request.Channels)
        {
            var sender = _senders.FirstOrDefault(s => s.Channel == channel);
            if (sender is null)
            {
                _logger.LogWarning("No sender registered for channel {Channel}", channel);
                continue;
            }

            try
            {
                var status = await sender.SendAsync(request, ct);
                _logger.LogInformation("Notification to {UserId} via {Channel}: {Status}", request.UserId, channel, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification to {UserId} via {Channel} threw", request.UserId, channel);
            }
        }
    }
}
