using Genkan.Lab.Domain.Dto;
using Genkan.Lab.Infrastructure.Interfaces;
using Genkan.Shared;
using Microsoft.Extensions.Logging;

namespace Genkan.Lab.Application.Services;

/// <summary>Builds the commands for the notification service and hands them to RabbitMQ.</summary>
internal static class PushCommands
{
    public static async Task<PushDispatch> SendAsync(
        IMessageBus bus, ILogger log, string kind, IReadOnlyList<string>? tokens, string? topic, PushMessage message, CancellationToken ct)
    {
        var command = new PushCommand(
            Guid.NewGuid(), kind, tokens, topic, new PushContent(message.Title, message.Body, message.Data), DateTimeOffset.UtcNow);
        try
        {
            await bus.PublishAsync(LabTopology.PushSendKey, command, ct);
            return new PushDispatch(command.CommandId, tokens?.Count ?? 1);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogError(ex, "Could not publish push command {CommandId} ({Kind})", command.CommandId, kind);
            return PushDispatch.Failed(PushDispatch.BrokerUnavailable);
        }
    }

    public static async Task<PushDispatch> SubscriptionAsync(
        IMessageBus bus, ILogger log, string topic, IReadOnlyList<string> tokens, bool subscribe, CancellationToken ct)
    {
        var command = new TopicSubscriptionCommand(Guid.NewGuid(), topic, tokens, subscribe, DateTimeOffset.UtcNow);
        try
        {
            await bus.PublishAsync(LabTopology.TopicSubscriptionKey, command, ct);
            return new PushDispatch(command.CommandId, tokens.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogError(ex, "Could not publish topic subscription command {CommandId} (topic {Topic})", command.CommandId, topic);
            return PushDispatch.Failed(PushDispatch.BrokerUnavailable);
        }
    }
}
