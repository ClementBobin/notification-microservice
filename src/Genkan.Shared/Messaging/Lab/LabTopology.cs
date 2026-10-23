using RabbitMQ.Client;

namespace Genkan.Shared.Messaging.Lab;

/// <summary>
/// RabbitMQ topology between lab and notification. One topic exchange:
///   * lab publishes DeviceRegistered / DevicesRemoved; every notification instance has its own exclusive queue bound to them
///     (so each instance updates its own cache);
///   * lab publishes commands to one shared durable queue, so each command is executed by exactly one notification instance;
///   * notification publishes DevicesInvalid and snapshot requests (RPC: ReplyTo + CorrelationId) to the lab queue.
/// Both services call <see cref="DeclareAsync"/> before publishing or consuming, so the declarations always match.
/// </summary>
/// <remarks>
/// Messages exchanged between the lab and notification services travel as JSON (<see cref="Genkan.Shared.Messaging.EventJson.Options"/>).
/// Lab owns the devices, groups and topic subscriptions; notification never reads them from the database:
/// it executes the commands it receives and keeps an in-memory copy of the device tokens fed by the events.
/// </remarks>
public static class LabTopology
{
    public const string Exchange = "genkan.lab";
    public const string DeadLetterExchange = "genkan.lab.dlx";
    public const string DeadQueue = "genkan.lab.dead";

    public const string LabQueue = "lab-service";
    public const string PushCommandQueue = "notification-push-commands";

    // Events published by lab (consumed from a per-instance queue of notification)
    public const string DeviceRegisteredKey = "device.registered";
    public const string DeviceRemovedKey = "device.removed";

    // Commands published by lab (consumed from PushCommandQueue)
    public const string PushSendKey = "push.send";
    public const string TopicSubscriptionKey = "push.topic-subscription";

    // Messages published by notification (consumed from LabQueue)
    public const string SnapshotRequestKey = "snapshot.request";
    public const string DeviceInvalidKey = "device.invalid";

    public static readonly string[] LabBindings = [SnapshotRequestKey, DeviceInvalidKey];
    public static readonly string[] PushCommandBindings = [PushSendKey, TopicSubscriptionKey];

    /// <summary>Idempotent: exchange, dead-letter exchange + queue, and the two durable queues with their bindings.</summary>
    public static async Task DeclareAsync(IChannel ch, CancellationToken ct)
    {
        await ch.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
        await ch.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Fanout, durable: true, cancellationToken: ct);

        await ch.QueueDeclareAsync(DeadQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await ch.QueueBindAsync(DeadQueue, DeadLetterExchange, "", cancellationToken: ct);

        var args = new Dictionary<string, object?> { ["x-dead-letter-exchange"] = DeadLetterExchange };
        await DeclareBoundQueueAsync(ch, LabQueue, LabBindings, args, ct);
        await DeclareBoundQueueAsync(ch, PushCommandQueue, PushCommandBindings, args, ct);
    }

    private static async Task DeclareBoundQueueAsync(IChannel ch, string queue, string[] bindings, Dictionary<string, object?> args, CancellationToken ct)
    {
        await ch.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, arguments: args, cancellationToken: ct);
        foreach (var key in bindings)
            await ch.QueueBindAsync(queue, Exchange, key, cancellationToken: ct);
    }
}
