using System.Text.Json;

namespace Genkan.Shared.Messaging;

/// <summary>Event envelope published on the topic exchange (see section 6 of the spec).</summary>
public sealed record EventEnvelope(
    Guid EventId,
    string Type,
    DateTimeOffset OccurredAt,
    IReadOnlyList<Guid> RecipientUserIds,
    JsonElement Data);
