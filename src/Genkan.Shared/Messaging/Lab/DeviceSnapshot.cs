namespace Genkan.Shared.Messaging.Lab;

/// <summary>
/// The answer to a <see cref="LabTopology.SnapshotRequestKey"/> (<c>snapshot.request</c>) message. Lab -> the ONE notification instance that asked.
/// The request is an RPC ("remote call over messages"): it carries a <c>ReplyTo</c> queue and a <c>CorrelationId</c>, and lab publishes this
/// reply directly to that queue. It gives a freshly started instance its first copy of every device, and is asked again every few minutes
/// to repair a cache that missed <c>device.*</c> events while RabbitMQ or the instance was down.
/// </summary>
/// <param name="GeneratedAt">Taken by lab before it reads the database (lab clock). A live event with a later timestamp wins over the snapshot.</param>
/// <param name="Devices">Every registered device.</param>
public sealed record DeviceSnapshot(DateTimeOffset GeneratedAt, IReadOnlyList<DeviceSnapshotItem> Devices);
