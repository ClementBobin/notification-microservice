namespace Genkan.Shared.Messaging.Lab;

/// <summary>One device inside a <see cref="DeviceSnapshot"/>: same fields as <see cref="DeviceRegistered"/>, without the timestamp.</summary>
public sealed record DeviceSnapshotItem(Guid UserId, string Token, string Platform);
