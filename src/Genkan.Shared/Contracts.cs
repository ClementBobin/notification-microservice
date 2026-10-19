using System.Text.Json;

namespace Genkan.Shared;

public static class Roles
{
    public const string User = "User";
    public const string Reception = "Reception";
    public const string Admin = "Admin";
    public static readonly string[] All = [User, Reception, Admin];
}

public static class GatewayHeaders
{
    public const string UserId = "X-User-Id";
    public const string UserRole = "X-User-Role";
    public const string UserEmail = "X-User-Email";
    public const string CorrelationId = "X-Correlation-Id";
}

/// <summary>Event envelope published on the topic exchange (see section 6 of the spec).</summary>
public sealed record EventEnvelope(
    Guid EventId,
    string Type,
    DateTimeOffset OccurredAt,
    IReadOnlyList<Guid> RecipientUserIds,
    JsonElement Data);

public static class EventJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}

public static class Topology
{
    public const string Exchange = "workplace.events";
    public const string DeadLetterExchange = "workplace.events.dlx";
    public const string NotificationQueue = "notification-service";
    public const string NotificationDeadQueue = "notification-service.dead";
    public static readonly string[] NotificationBindings = ["booking.#", "access.#", "visitor.#", "alert.#"];
}
