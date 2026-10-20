using Genkan.Notification.Domain.Dto;

namespace Genkan.Notification.Application.Interfaces;

public interface ITopicPushService
{
    Task<PushResult> SendAsync(string topic, PushMessage message, CancellationToken ct);
    /// <summary>Stores the subscriptions and subscribes the users' devices; returns how many devices were subscribed.</summary>
    Task<int> SubscribeAsync(string topic, IReadOnlyList<Guid> userIds, CancellationToken ct);
    Task<int> UnsubscribeAsync(string topic, IReadOnlyList<Guid> userIds, CancellationToken ct);
    /// <summary>Subscribes a newly registered (or rotated) device token to the topics its user is subscribed to.</summary>
    Task SubscribeDeviceToUserTopicsAsync(Guid userId, string token, CancellationToken ct);
}
