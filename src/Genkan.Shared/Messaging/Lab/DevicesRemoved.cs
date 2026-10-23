namespace Genkan.Shared.Messaging.Lab;

/// <summary>Lab -> every notification instance: these device tokens no longer exist. At is the lab clock.</summary>
public sealed record DevicesRemoved(IReadOnlyList<string> Tokens, DateTimeOffset At);
