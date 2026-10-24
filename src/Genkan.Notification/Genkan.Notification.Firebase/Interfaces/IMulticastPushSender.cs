using Genkan.Notification.Domain.Dto;

namespace Genkan.Notification.Firebase.Interfaces;

/// <summary>Sends one message to many device tokens (batched by 500, the FCM limit). Also used for device groups.</summary>
public interface IMulticastPushSender
{
    Task<PushResult> SendAsync(IReadOnlyList<string> tokens, PushMessage message, CancellationToken ct);
}
