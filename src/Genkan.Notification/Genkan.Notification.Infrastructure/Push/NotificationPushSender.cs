using Genkan.Notification.Domain.Dto;
using Genkan.Shared.Entities;
using Genkan.Notification.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace Genkan.Notification.Infrastructure.Push;

/// <summary>
/// Push used for events received from RabbitMQ: all devices of the recipient (every device for a broadcast), sent as a multicast.
/// Whether the multicast goes to Firebase or only to the log is decided by the registered <see cref="IMulticastPushSender"/>.
/// </summary>
public sealed class NotificationPushSender(IDeviceTokenRepository devices, IMulticastPushSender multicast, ILogger<NotificationPushSender> log) : IPushSender
{
    public async Task<int> SendAsync(NotificationLog n, CancellationToken ct)
    {
        var tokens = await devices.GetTokensAsync(n.IsBroadcast ? null : n.RecipientUserId, ct);
        if (tokens.Count == 0)
        {
            log.LogInformation("No registered device for notification {Id}, nothing to push", n.Id);
            return 0;
        }

        var message = new PushMessage(n.Title, n.Body, new Dictionary<string, string>
        {
            ["notificationId"] = n.Id.ToString(),
            ["eventId"] = n.EventId.ToString(),
            ["type"] = n.EventType,
        });

        var result = await multicast.SendAsync(tokens, message, ct);
        if (result.InvalidTokens.Count > 0)
        {
            await devices.RemoveTokensAsync(result.InvalidTokens, ct);
            log.LogInformation("Removed {Count} unregistered device token(s)", result.InvalidTokens.Count);
        }

        if (result.Accepted == 0 && result.Error is not null) throw new InvalidOperationException(result.Error);
        return result.Accepted;
    }
}
