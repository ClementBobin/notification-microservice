namespace Genkan.Shared.Messaging.Lab;

/// <summary>
/// Routing key <see cref="LabTopology.DeviceRegisteredKey"/> (<c>device.registered</c>). Lab -> EVERY notification instance (event: "this happened").
/// A phone registered its FCM token (first login, new login, or FCM rotated the token), or the token moved to another user.
/// Each notification instance stores it in its in-memory device cache, so it knows which tokens belong to which user without asking lab.
/// </summary>
/// <param name="UserId">Owner of the device.</param>
/// <param name="Token">The FCM registration token of the device.</param>
/// <param name="Platform">android, ios or web.</param>
/// <param name="At">When it happened (lab clock). Used to ignore a stale event that arrives after a newer one.</param>
public sealed record DeviceRegistered(Guid UserId, string Token, string Platform, DateTimeOffset At);
