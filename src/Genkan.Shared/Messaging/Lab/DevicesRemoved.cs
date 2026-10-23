namespace Genkan.Shared.Messaging.Lab;

/// <summary>
/// Routing key <see cref="LabTopology.DeviceRemovedKey"/> (<c>device.removed</c>). Lab -> EVERY notification instance (event: "this happened").
/// These device tokens no longer exist: the user logged out, or lab deleted them after a <see cref="DevicesInvalid"/> report.
/// Each notification instance removes them from its in-memory device cache.
/// </summary>
/// <param name="Tokens">The FCM tokens that were removed.</param>
/// <param name="At">When it happened (lab clock). Used to ignore a stale event that arrives after a newer one.</param>
public sealed record DevicesRemoved(IReadOnlyList<string> Tokens, DateTimeOffset At);
