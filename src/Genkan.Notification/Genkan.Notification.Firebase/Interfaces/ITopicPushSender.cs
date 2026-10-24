using Genkan.Notification.Domain.Dto;

namespace Genkan.Notification.Firebase.Interfaces;

public interface ITopicPushSender
{
    Task<PushResult> SendAsync(string topic, PushMessage message, CancellationToken ct);
    /// <summary>Subscribes the tokens to the topic; returns how many were subscribed.</summary>
    Task<int> SubscribeAsync(IReadOnlyList<string> tokens, string topic, CancellationToken ct);
    Task<int> UnsubscribeAsync(IReadOnlyList<string> tokens, string topic, CancellationToken ct);
}
