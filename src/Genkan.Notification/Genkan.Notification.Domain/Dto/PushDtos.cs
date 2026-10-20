using System.Text.RegularExpressions;

namespace Genkan.Notification.Domain.Dto;

/// <summary>Content of a push. Shared by every kind of send (individual, multicast, device group, topic).</summary>
public sealed record PushMessage(string Title, string Body, IReadOnlyDictionary<string, string>? Data = null)
{
    public bool IsValid() => !string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(Body);
}

/// <summary>
/// Outcome of a send. Accepted = device messages accepted by FCM (for a topic: 1 = the message was accepted, FCM does the fan-out).
/// InvalidTokens = tokens FCM reported as unregistered (they are removed by the application layer).
/// </summary>
public sealed record PushResult(int Accepted, int Failed, IReadOnlyList<string> InvalidTokens, string? Error = null, string? MessageId = null)
{
    public const string NoDevice = "no_registered_device";

    public static PushResult Empty(string reason) => new(0, 0, [], reason);
}

/// <summary>Target: one device token, or one user (their most recently used device).</summary>
public sealed record IndividualPushRequest(string? Token, Guid? UserId, PushMessage Message);

/// <summary>Targets: an ad-hoc list of tokens and/or users (all their devices).</summary>
public sealed record MulticastPushRequest(IReadOnlyList<string>? Tokens, IReadOnlyList<Guid>? UserIds, PushMessage Message);

/// <summary>Body of the "set group members" and "subscribe / unsubscribe topic" calls.</summary>
public sealed record UserIdsRequest(IReadOnlyList<Guid> UserIds);

public sealed record DeviceGroupDto(string Name, int MemberCount);

/// <summary>Public client-side identifiers (the content of google-services.json), never the service-account key.</summary>
public sealed record FirebaseClientConfigDto(string ApiKey, string ApplicationId, string MessagingSenderId, string ProjectId);

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
