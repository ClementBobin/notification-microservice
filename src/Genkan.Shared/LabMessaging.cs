using RabbitMQ.Client;

namespace Genkan.Shared;

// Messages exchanged between the lab and notification services over RabbitMQ (JSON, EventJson.Options).
// Lab owns the devices, groups and topic subscriptions; notification never reads them from the database:
// it executes the commands it receives and keeps an in-memory copy of the device tokens fed by the events below.

/// <summary>What a push shows on the device (the wire counterpart of each service's own PushMessage model).</summary>
public sealed record PushContent(string Title, string Body, IReadOnlyDictionary<string, string>? Data = null);

public static class PushKinds
{
    /// <summary><see cref="PushCommand.Tokens"/> holds exactly one token.</summary>
    public const string Individual = "individual";
    /// <summary><see cref="PushCommand.Tokens"/> holds the tokens (already resolved from users / device groups by lab).</summary>
    public const string Multicast = "multicast";
    /// <summary><see cref="PushCommand.Topic"/> holds the FCM topic.</summary>
    public const string Topic = "topic";
}

public static class PushTopics
{
    /// <summary>
    /// Every registered device is subscribed to it by lab when its token is registered, so a push to everyone is one topic send.
    /// FCM creates a topic when its first device subscribes: nothing has to be created beforehand.
    /// </summary>
    public const string Broadcast = "broadcast";
}

/// <summary>Lab -> notification: send this push. Lab has already resolved users and groups to device tokens.</summary>
public sealed record PushCommand(Guid CommandId, string Kind, IReadOnlyList<string>? Tokens, string? Topic, PushContent Message, DateTimeOffset At);

/// <summary>Lab -> notification: (un)subscribe these device tokens to an FCM topic.</summary>
public sealed record TopicSubscriptionCommand(Guid CommandId, string Topic, IReadOnlyList<string> Tokens, bool Subscribe, DateTimeOffset At);

/// <summary>Lab -> every notification instance: a device token was registered, or moved to another user. At is the lab clock.</summary>
public sealed record DeviceRegistered(Guid UserId, string Token, string Platform, DateTimeOffset At);

/// <summary>Lab -> every notification instance: these device tokens no longer exist. At is the lab clock.</summary>
public sealed record DevicesRemoved(IReadOnlyList<string> Tokens, DateTimeOffset At);

/// <summary>Notification -> lab: FCM reported these tokens as unregistered. Lab deletes them and announces DevicesRemoved.</summary>
public sealed record DevicesInvalid(IReadOnlyList<string> Tokens, DateTimeOffset At);

public sealed record DeviceSnapshotItem(Guid UserId, string Token, string Platform);

/// <summary>Lab -> the notification instance that asked (RPC reply): every device as it was at GeneratedAt (lab clock).</summary>
public sealed record DeviceSnapshot(DateTimeOffset GeneratedAt, IReadOnlyList<DeviceSnapshotItem> Devices);

/// <summary>
/// RabbitMQ topology between lab and notification. One topic exchange:
///   * lab publishes DeviceRegistered / DevicesRemoved; every notification instance has its own exclusive queue bound to them
///     (so each instance updates its own cache);
///   * lab publishes commands to one shared durable queue, so each command is executed by exactly one notification instance;
///   * notification publishes DevicesInvalid and snapshot requests (RPC: ReplyTo + CorrelationId) to the lab queue.
/// Both services call <see cref="DeclareAsync"/> before publishing or consuming, so the declarations always match.
/// </summary>
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
