namespace Genkan.Shared.Messaging.Lab;

/// <summary>Notification -> lab: FCM reported these tokens as unregistered. Lab deletes them and announces DevicesRemoved.</summary>
public sealed record DevicesInvalid(IReadOnlyList<string> Tokens, DateTimeOffset At);
