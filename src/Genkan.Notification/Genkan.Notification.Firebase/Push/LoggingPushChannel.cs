using Genkan.Notification.Domain.Dto;
using Genkan.Notification.Firebase.Interfaces;
using Microsoft.Extensions.Logging;

namespace Genkan.Notification.Firebase.Push;

/// <summary>Used when Firebase:Enabled is false (local development): logs every push instead of sending it.</summary>
public sealed class LoggingPushChannel(ILogger<LoggingPushChannel> log) : IMulticastPushSender, ITopicPushSender
{
    Task<PushResult> IMulticastPushSender.SendAsync(IReadOnlyList<string> tokens, PushMessage message, CancellationToken ct)
    {
        log.LogInformation("PUSH multicast to {Count} device(s): {Title} - {Body}", tokens.Count, message.Title, message.Body);
        return Task.FromResult(new PushResult(tokens.Count, 0, []));
    }

    Task<PushResult> ITopicPushSender.SendAsync(string topic, PushMessage message, CancellationToken ct)
    {
        log.LogInformation("PUSH topic {Topic}: {Title} - {Body}", topic, message.Title, message.Body);
        return Task.FromResult(new PushResult(1, 0, []));
    }

    public Task<int> SubscribeAsync(IReadOnlyList<string> tokens, string topic, CancellationToken ct)
    {
        log.LogInformation("PUSH subscribe {Count} device(s) to topic {Topic}", tokens.Count, topic);
        return Task.FromResult(tokens.Count);
    }

    public Task<int> UnsubscribeAsync(IReadOnlyList<string> tokens, string topic, CancellationToken ct)
    {
        log.LogInformation("PUSH unsubscribe {Count} device(s) from topic {Topic}", tokens.Count, topic);
        return Task.FromResult(tokens.Count);
    }
}
