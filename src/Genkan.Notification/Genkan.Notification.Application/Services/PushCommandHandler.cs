using Genkan.Notification.Domain.Dto;
using Genkan.Notification.Infrastructure.Interfaces;
using Genkan.Shared;
using Microsoft.Extensions.Logging;

namespace Genkan.Notification.Application.Services;

/// <summary>
/// Executes what the lab service asks for over RabbitMQ: individual / multicast / topic pushes (to FCM, or to the log when Firebase is disabled)
/// and topic (un)subscriptions. Lab has already resolved users and groups to device tokens.
/// </summary>
public sealed class PushCommandHandler(
    IIndividualPushSender individual, IMulticastPushSender multicast, ITopicPushSender topic, ILabBus lab, ILogger<PushCommandHandler> log) : IPushCommandHandler
{
    public async Task HandleAsync(PushCommand command, CancellationToken ct)
    {
        var message = new PushMessage(command.Message.Title, command.Message.Body, command.Message.Data);
        var tokens = command.Tokens ?? [];

        PushResult result;
        switch (command.Kind)
        {
            case PushKinds.Individual when tokens.Count > 0:
                result = await individual.SendAsync(tokens[0], message, ct);
                break;
            case PushKinds.Multicast when tokens.Count > 0:
                result = await multicast.SendAsync(tokens, message, ct);
                break;
            case PushKinds.Topic when !string.IsNullOrWhiteSpace(command.Topic):
                result = await topic.SendAsync(command.Topic, message, ct);
                break;
            default:
                log.LogWarning("Push command {CommandId} ignored: unsupported or incomplete ({Kind})", command.CommandId, command.Kind);
                return;
        }

        if (result.Accepted > 0)
            log.LogInformation("Push command {CommandId} ({Kind}): {Accepted} accepted, {Failed} failed", command.CommandId, command.Kind, result.Accepted, result.Failed);
        else
            log.LogWarning("Push command {CommandId} ({Kind}) not accepted: {Error}", command.CommandId, command.Kind, result.Error ?? "unknown");

        if (result.InvalidTokens.Count > 0)
            await lab.PublishAsync(LabTopology.DeviceInvalidKey, new DevicesInvalid(result.InvalidTokens, DateTimeOffset.UtcNow), ct);
    }

    public async Task HandleAsync(TopicSubscriptionCommand command, CancellationToken ct)
    {
        if (command.Tokens.Count == 0) return;

        var done = command.Subscribe
            ? await topic.SubscribeAsync(command.Tokens, command.Topic, ct)
            : await topic.UnsubscribeAsync(command.Tokens, command.Topic, ct);
        log.LogInformation("Topic command {CommandId}: {Done}/{Total} device(s) {Action} topic {Topic}",
            command.CommandId, done, command.Tokens.Count, command.Subscribe ? "subscribed to" : "unsubscribed from", command.Topic);
    }
}
