namespace Genkan.Shared.Messaging.Lab;

/// <summary>
/// Routing key <see cref="LabTopology.TopicSubscriptionKey"/> (<c>push.topic-subscription</c>). Lab -> ONE notification instance:
/// "(un)subscribe these devices to this FCM topic". Sent by lab when a device registers (subscribe) or logs out (unsubscribe),
/// because FCM ties topic subscriptions to the device token, not to the user.
/// </summary>
/// <param name="CommandId">Unique id of the command, only used in logs.</param>
/// <param name="Topic">The FCM topic (for example <see cref="PushTopics.Broadcast"/>).</param>
/// <param name="Tokens">The device tokens to subscribe or unsubscribe.</param>
/// <param name="Subscribe">True to subscribe, false to unsubscribe.</param>
/// <param name="At">When lab created the command (lab clock).</param>
public sealed record TopicSubscriptionCommand(Guid CommandId, string Topic, IReadOnlyList<string> Tokens, bool Subscribe, DateTimeOffset At);
