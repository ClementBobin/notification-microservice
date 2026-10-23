namespace Genkan.Shared.Messaging.Lab;

/// <summary>Lab -> notification: send this push. Lab has already resolved users and groups to device tokens.</summary>
public sealed record PushCommand(Guid CommandId, string Kind, IReadOnlyList<string>? Tokens, string? Topic, PushContent Message, DateTimeOffset At);
