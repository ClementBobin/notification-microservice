using Genkan.Shared.Messaging.Lab;

namespace Genkan.Notification.Infrastructure.Interfaces;

/// <summary>
/// The device tokens known to this instance, kept in memory. They are owned by the lab service and reach this cache over RabbitMQ
/// (live events + a full snapshot at startup and at a fixed interval); the notification service never reads them from the database.
/// </summary>
public interface IDeviceCache
{
    /// <summary>True once a first snapshot from lab was applied.</summary>
    bool IsReady { get; }

    /// <summary>Completes when the first snapshot was applied (or throws when cancelled).</summary>
    Task WaitUntilReadyAsync(CancellationToken ct);

    /// <summary>Tokens of one user, or of every user when <paramref name="userId"/> is null (broadcast).</summary>
    IReadOnlyList<string> GetTokens(Guid? userId);

    /// <summary>Registers the token for the user (re-assigns it when it belonged to another user). <paramref name="at"/> is the lab clock.</summary>
    void Upsert(Guid userId, string token, string platform, DateTimeOffset at);

    void Remove(IEnumerable<string> tokens, DateTimeOffset at);

    /// <summary>Replaces the content with the snapshot, except for changes announced after it was taken.</summary>
    void ApplySnapshot(DeviceSnapshot snapshot);
}
