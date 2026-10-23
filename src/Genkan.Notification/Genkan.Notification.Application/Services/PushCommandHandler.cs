using Genkan.Notification.Domain.Dto;
using Genkan.Notification.Infrastructure.Interfaces;
using Genkan.Shared.Messaging.Lab;
using Microsoft.Extensions.Logging;

namespace Genkan.Notification.Application.Services;

/// <summary>
/// Executes what the lab service asks for over RabbitMQ: individual / multicast / topic pushes (to FCM, or to the log when Firebase is disabled)
/// and topic (un)subscriptions. Lab has already resolved users and groups to device tokens.
/// </summary>
public sealed class PushCommandHandler(IMulticastPushSender multicast, ILogger<PushCommandHandler> log) : IPushCommandHandler
{
    public async Task HandleAsync(PushCommand command, CancellationToken ct)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        var tokens = command.Tokens ?? [];
        if (tokens.Count == 0)
        {
            log.LogWarning("Push command {CommandId} has no tokens", command.CommandId);
            return;
        }

        var message = new PushMessage(command.Message.Title, command.Message.Body, command.Message.Data);
        var result = await multicast.SendAsync(tokens, message, ct);

        if (result.Accepted > 0)
            log.LogInformation("Push command {CommandId}: {Accepted} accepted, {Failed} failed", command.CommandId, result.Accepted, result.Failed);
        else
            log.LogWarning("Push command {CommandId} not accepted: {Error}", command.CommandId, result.Error ?? "unknown");

        if (result.InvalidTokens.Count > 0)
            log.LogWarning("Push command {CommandId}: {Invalid} invalid tokens", command.CommandId, result.InvalidTokens.Count);
    }

    // Topics are no longer used: subscriptions are tracked in the database and expanded to tokens by lab.
    public Task HandleAsync(TopicSubscriptionCommand command, CancellationToken ct)
    {
        log.LogInformation("Topic command {CommandId} ignored: topics are not used", command.CommandId);
        return Task.CompletedTask;
    }
}