using Genkan.Notification.Application.Interfaces;
using Genkan.Notification.Domain.Dto;
using Genkan.Notification.Infrastructure.Interfaces;

namespace Genkan.Notification.Application.Services;

/// <summary>
/// Topic notification: publish to an FCM topic. Subscriptions are managed here (admin only) and stored per user,
/// so a device that registers a new token is re-subscribed to its user's topics.
/// </summary>
public sealed class TopicPushService(ITopicPushSender fcm, ITopicSubscriptionRepository subscriptions, IDeviceTokenRepository devices) : ITopicPushService
{
    public Task<PushResult> SendAsync(string topic, PushMessage message, CancellationToken ct) =>
        fcm.SendAsync(topic, message, ct);

    public async Task<int> SubscribeAsync(string topic, IReadOnlyList<Guid> userIds, CancellationToken ct)
    {
        await subscriptions.AddAsync(topic, userIds, ct);
        var tokens = await devices.GetTokensForUsersAsync(userIds, ct);
        return tokens.Count == 0 ? 0 : await fcm.SubscribeAsync(tokens, topic, ct);
    }

    public async Task<int> UnsubscribeAsync(string topic, IReadOnlyList<Guid> userIds, CancellationToken ct)
    {
        await subscriptions.RemoveAsync(topic, userIds, ct);
        var tokens = await devices.GetTokensForUsersAsync(userIds, ct);
        return tokens.Count == 0 ? 0 : await fcm.UnsubscribeAsync(tokens, topic, ct);
    }

    public async Task SubscribeDeviceToUserTopicsAsync(Guid userId, string token, CancellationToken ct)
    {
        foreach (var topic in await subscriptions.GetTopicsAsync(userId, ct))
            await fcm.SubscribeAsync([token], topic, ct);
    }
}
