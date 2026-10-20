using FirebaseAdmin.Messaging;
using Genkan.Notification.Domain.Entities;
using Genkan.Notification.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace Genkan.Notification.Infrastructure.Push;

/// <summary>Sends the notification through Firebase Cloud Messaging to every registered device of the recipient (all devices for a broadcast).</summary>
public sealed class FcmPushSender(FirebaseMessaging fcm, IDeviceTokenRepository devices, ILogger<FcmPushSender> log) : IPushSender
{
    private const int BatchSize = 500; // FCM multicast limit

    public async Task<int> SendAsync(NotificationLog n, CancellationToken ct)
    {
        var tokens = await devices.GetTokensAsync(n.IsBroadcast ? null : n.RecipientUserId, ct);
        if (tokens.Count == 0)
        {
            log.LogInformation("No registered device for notification {Id}, nothing to push", n.Id);
            return 0;
        }

        var delivered = 0;
        var stale = new List<string>();
        string? lastError = null;

        foreach (var batch in tokens.Chunk(BatchSize))
        {
            var message = new MulticastMessage
            {
                Tokens = batch,
                // Fully qualified: inside Genkan.Notification.* the simple name "Notification" is our namespace.
                Notification = new FirebaseAdmin.Messaging.Notification { Title = n.Title, Body = n.Body },
                Data = new Dictionary<string, string>
                {
                    ["notificationId"] = n.Id.ToString(),
                    ["eventId"] = n.EventId.ToString(),
                    ["type"] = n.EventType,
                },
                Android = new AndroidConfig { Priority = Priority.High },
            };

            var response = await fcm.SendEachForMulticastAsync(message, ct);
            for (var i = 0; i < response.Responses.Count; i++)
            {
                var r = response.Responses[i];
                if (r.IsSuccess) { delivered++; continue; }
                if (r.Exception?.MessagingErrorCode == MessagingErrorCode.Unregistered) stale.Add(batch[i]);
                else lastError = r.Exception?.Message ?? "unknown FCM error";
            }
        }

        if (stale.Count > 0)
        {
            await devices.RemoveTokensAsync(stale, ct);
            log.LogInformation("Removed {Count} unregistered device token(s)", stale.Count);
        }

        if (delivered == 0 && lastError is not null) throw new InvalidOperationException(lastError);
        return delivered;
    }
}
