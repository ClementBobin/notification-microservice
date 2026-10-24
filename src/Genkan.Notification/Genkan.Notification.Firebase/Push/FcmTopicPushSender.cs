using FirebaseAdmin.Messaging;
using Genkan.Notification.Domain.Dto;
using Genkan.Notification.Firebase.Interfaces;

namespace Genkan.Notification.Firebase.Push;

/// <summary>Topic notification: publish to a topic and manage which tokens are subscribed to it.</summary>
public sealed class FcmTopicPushSender(FirebaseMessaging fcm) : ITopicPushSender
{
    private const int BatchSize = 1000; // FCM limit per subscribe / unsubscribe request

    public async Task<PushResult> SendAsync(string topic, PushMessage message, CancellationToken ct)
    {
        try
        {
            var id = await fcm.SendAsync(FcmMessageBuilder.ForTopic(topic, message), ct);
            return new PushResult(1, 0, [], null, id);
        }
        catch (FirebaseMessagingException ex)
        {
            return new PushResult(0, 1, [], ex.Message);
        }
    }

    public async Task<int> SubscribeAsync(IReadOnlyList<string> tokens, string topic, CancellationToken ct)
    {
        var subscribed = 0;
        foreach (var batch in tokens.Chunk(BatchSize))
        {
            ct.ThrowIfCancellationRequested();
            subscribed += (await fcm.SubscribeToTopicAsync(batch, topic)).SuccessCount;
        }
        return subscribed;
    }

    public async Task<int> UnsubscribeAsync(IReadOnlyList<string> tokens, string topic, CancellationToken ct)
    {
        var removed = 0;
        foreach (var batch in tokens.Chunk(BatchSize))
        {
            ct.ThrowIfCancellationRequested();
            removed += (await fcm.UnsubscribeFromTopicAsync(batch, topic)).SuccessCount;
        }
        return removed;
    }
}
