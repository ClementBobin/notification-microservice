namespace Genkan.Shared.Messaging.Lab;

/// <summary>
/// Routing key <see cref="LabTopology.PushSendKey"/> (<c>push.send</c>). Lab -> ONE notification instance: "send this push".
/// Lab has already turned users and device groups into device tokens, so notification only has to call FCM.
/// Notification then answers with <c>device.invalid</c> if FCM rejected some tokens.
/// </summary>
/// <param name="CommandId">Unique id of the command, only used in logs to follow it across services.</param>
/// <param name="Tokens">Device tokens to send to (individual: exactly one, multicast: many). Null for a topic push.</param>
/// <param name="Topic">FCM topic to send to. Null for individual and multicast pushes.</param>
/// <param name="Message">What the device will show.</param>
/// <param name="At">When lab created the command (lab clock).</param>
public sealed record PushCommand(Guid CommandId, IReadOnlyList<string>? Tokens, string? Topic, PushContent Message, DateTimeOffset At);
