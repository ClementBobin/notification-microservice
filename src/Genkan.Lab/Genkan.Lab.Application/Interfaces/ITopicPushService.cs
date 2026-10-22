using Genkan.Lab.Domain.Dto;

namespace Genkan.Lab.Application.Interfaces;

public interface ITopicPushService
{
    Task<PushDispatch> SendAsync(string topic, PushMessage message, CancellationToken ct);
    /// <summary>Stores the subscriptions and asks the notification service to subscribe the users' devices. Targets = devices addressed (0 = the users have none yet).</summary>
    Task<PushDispatch> SubscribeAsync(string topic, IReadOnlyList<Guid> userIds, CancellationToken ct);
    Task<PushDispatch> UnsubscribeAsync(string topic, IReadOnlyList<Guid> userIds, CancellationToken ct);
    /// <summary>Subscribes every registered device to the broadcast topic (for the devices registered before devices were subscribed automatically). Targets = devices addressed.</summary>
    Task<PushDispatch> SyncBroadcastAsync(CancellationToken ct);
}
