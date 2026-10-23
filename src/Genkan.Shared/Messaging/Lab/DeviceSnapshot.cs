namespace Genkan.Shared.Messaging.Lab;

/// <summary>Lab -> the notification instance that asked (RPC reply): every device as it was at GeneratedAt (lab clock).</summary>
public sealed record DeviceSnapshot(DateTimeOffset GeneratedAt, IReadOnlyList<DeviceSnapshotItem> Devices);
