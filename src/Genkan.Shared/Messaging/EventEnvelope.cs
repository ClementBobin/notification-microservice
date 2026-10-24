using System.Text.Json;

namespace Genkan.Shared.Messaging;

/// <summary>
/// The message published on the <see cref="Topology.Exchange"/> exchange (see section 6 of the spec) whenever something happens in the
/// workplace: a room is booked, a door denies access, a visitor arrives, an alert is raised. The notification service reads it and
/// turns it into a notification (stored in the database, then pushed through FCM). The routing key of the message is <see cref="Type"/>.
/// </summary>
/// <param name="EventId">Unique id of the event. With the recipient it makes the processing idempotent: a duplicate delivery is skipped.</param>
/// <param name="Type">What happened, for example <c>booking.created</c>. It is also the RabbitMQ routing key, so it decides which queues get the event.</param>
/// <param name="OccurredAt">When it happened.</param>
/// <param name="RecipientUserIds">The users to notify. Empty is allowed only for <c>alert.*</c> events, which then go to everybody (broadcast).</param>
/// <param name="Data">Event specific details (room, door, visitor, title...) read by the notification templates.</param>
public sealed record EventEnvelope(
    Guid EventId,
    string Type,
    DateTimeOffset OccurredAt,
    IReadOnlyList<Guid> RecipientUserIds,
    JsonElement Data);
