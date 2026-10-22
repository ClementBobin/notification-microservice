using Genkan.Lab.Application.Interfaces;
using Genkan.Lab.Domain.Dto;
using Genkan.Lab.Infrastructure.Interfaces;
using Genkan.Shared;
using Microsoft.Extensions.Logging;

namespace Genkan.Lab.Application.Services;

/// <summary>
/// Topic notification: publish to an FCM topic (done by the notification service). Subscriptions are managed here (admin only)
/// and stored per user, so a device that registers a new token is re-subscribed to its user's topics.
/// </summary>
public sealed class TopicPushService(ITopicSubscriptionRepository subscriptions, IDeviceTokenRepository devices, IMessageBus bus, ILogger<TopicPushService> log) : ITopicPushService
{
    public Task<PushDispatch> SendAsync(string topic, PushMessage message, CancellationToken ct) =>
        PushCommands.SendAsync(bus, log, PushKinds.Topic, null, topic, message, ct);

    public async Task<PushDispatch> SubscribeAsync(string topic, IReadOnlyList<Guid> userIds, CancellationToken ct)
    {
        await subscriptions.AddAsync(topic, userIds, ct);
        var tokens = await devices.GetTokensForUsersAsync(userIds, ct);
        return tokens.Count == 0
            ? new PushDispatch(Guid.Empty, 0)
            : await PushCommands.SubscriptionAsync(bus, log, topic, tokens, true, ct);
    }

    public async Task<PushDispatch> UnsubscribeAsync(string topic, IReadOnlyList<Guid> userIds, CancellationToken ct)
    {
        await subscriptions.RemoveAsync(topic, userIds, ct);
        var tokens = await devices.GetTokensForUsersAsync(userIds, ct);
        return tokens.Count == 0
            ? new PushDispatch(Guid.Empty, 0)
            : await PushCommands.SubscriptionAsync(bus, log, topic, tokens, false, ct);
    }

    public async Task<PushDispatch> SyncBroadcastAsync(CancellationToken ct)
    {
        var tokens = (await devices.GetAllAsync(ct)).Select(d => d.Token).Distinct().ToList();
        if (tokens.Count == 0) return new PushDispatch(Guid.Empty, 0);

        // One command per 1000 tokens (the FCM limit per request): a single huge message would not fit in RabbitMQ.
        var addressed = 0;
        var last = new PushDispatch(Guid.Empty, 0);
        foreach (var batch in tokens.Chunk(1000))
        {
            last = await PushCommands.SubscriptionAsync(bus, log, PushTopics.Broadcast, batch, true, ct);
            if (last.Error is not null) return last;
            addressed += batch.Length;
        }
        return new PushDispatch(last.CommandId, addressed);
    }
}
