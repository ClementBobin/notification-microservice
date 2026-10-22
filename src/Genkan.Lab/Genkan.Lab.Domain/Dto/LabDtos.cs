using System.Text.RegularExpressions;

namespace Genkan.Lab.Domain.Dto;

public sealed record RegisterDeviceRequest(string Token, string Platform);

public sealed record UnregisterDeviceRequest(string Token);

/// <summary>Content of a push. Shared by every kind of send (individual, multicast, device group, topic).</summary>
public sealed record PushMessage(string Title, string Body, IReadOnlyDictionary<string, string>? Data = null)
{
    public bool IsValid() => !string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(Body);
}

/// <summary>Target: one device token, or one user (their most recently used device).</summary>
public sealed record IndividualPushRequest(string? Token, Guid? UserId, PushMessage Message);

/// <summary>Targets: an ad-hoc list of tokens and/or users (all their devices).</summary>
public sealed record MulticastPushRequest(IReadOnlyList<string>? Tokens, IReadOnlyList<Guid>? UserIds, PushMessage Message);

/// <summary>Body of the "set group members" and "subscribe / unsubscribe topic" calls.</summary>
public sealed record UserIdsRequest(IReadOnlyList<Guid> UserIds);

public sealed record DeviceGroupDto(string Name, int MemberCount);

/// <summary>
/// Outcome of handing a push to RabbitMQ. The notification service sends it to FCM afterwards, so there is no per-device result here:
/// Targets = number of device tokens addressed (1 for a topic), CommandId = the id to look for in the notification service's logs.
/// </summary>
public sealed record PushDispatch(Guid CommandId, int Targets, string? Error = null)
{
    public const string NoDevice = "no_registered_device";
    public const string BrokerUnavailable = "message_broker_unavailable";

    public static PushDispatch Failed(string error) => new(Guid.Empty, 0, error);
}

public static partial class PushRules
{
    // FCM topic names: [a-zA-Z0-9-_.~%]+
    [GeneratedRegex("^[a-zA-Z0-9-_.~%]{1,900}$")]
    private static partial Regex TopicRegex();

    [GeneratedRegex("^[a-zA-Z0-9_.-]{1,100}$")]
    private static partial Regex GroupRegex();

    public static bool IsValidTopic(string value) => TopicRegex().IsMatch(value);
    public static bool IsValidGroupName(string value) => GroupRegex().IsMatch(value);
}
