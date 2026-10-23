using Genkan.Notification.Domain.Dto;
using Genkan.Notification.Infrastructure.Interfaces;
using Genkan.Shared.Messaging.Lab;
using Genkan.Shared.Entities;
using Microsoft.Extensions.Logging;

namespace Genkan.Notification.Infrastructure.Push;

/// <summary>
/// Push used for events received from RabbitMQ: all devices of the recipient (every device for a broadcast), sent as a multicast.
/// The devices come from the in-memory <see cref="IDeviceCache"/> fed by the lab service, never from the database.
/// Whether the multicast goes to Firebase or only to the log is decided by the registered <see cref="IMulticastPushSender"/>.
/// </summary>
public sealed class NotificationPushSender(IDeviceCache devices, IMulticastPushSender multicast, ILabBus lab, ILogger<NotificationPushSender> log) : IPushSender
{
    public async Task<int> SendAsync(NotificationLog n, CancellationToken ct)
    {
        var tokens = devices.GetTokens(n.IsBroadcast ? null : n.RecipientUserId);
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
        await ReportInvalidAsync(result, ct);

        if (result.Accepted == 0 && result.Error is not null) throw new InvalidOperationException(result.Error);
        return result.Accepted;
    }

    private async Task ReportInvalidAsync(PushResult result, CancellationToken ct)
    {
        if (result.InvalidTokens.Count == 0) return;
        try
        {
            // The lab service owns the devices: it deletes them and tells every instance (this one included).
            await lab.PublishAsync(LabTopology.DeviceInvalidKey, new DevicesInvalid(result.InvalidTokens, DateTimeOffset.UtcNow), ct);
            log.LogInformation("Reported {Count} unregistered device token(s) to the lab service", result.InvalidTokens.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogWarning(ex, "Could not report {Count} unregistered device token(s) to the lab service", result.InvalidTokens.Count);
        }
    }
}
