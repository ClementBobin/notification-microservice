namespace Genkan.Shared.Messaging.Lab;

/// <summary>Lab -> every notification instance: a device token was registered, or moved to another user. At is the lab clock.</summary>
public sealed record DeviceRegistered(Guid UserId, string Token, string Platform, DateTimeOffset At);
