namespace Genkan.Shared.Messaging.Lab;

/// <summary>Lab -> notification: (un)subscribe these device tokens to an FCM topic.</summary>
public sealed record TopicSubscriptionCommand(Guid CommandId, string Topic, IReadOnlyList<string> Tokens, bool Subscribe, DateTimeOffset At);
