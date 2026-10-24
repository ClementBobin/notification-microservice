namespace Genkan.Notification.Domain.Dto;

/// <summary>Content of a push, as sent to FCM.</summary>
public sealed record PushMessage(string Title, string Body, IReadOnlyDictionary<string, string>? Data = null)
{
    public bool IsValid() => !string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(Body);
}

/// <summary>
/// Outcome of a send. Accepted = device messages accepted by FCM (for a topic: 1 = the message was accepted, FCM does the fan-out).
/// InvalidTokens = tokens FCM reported as unregistered (the application layer reports them to the lab service, which deletes them).
/// </summary>
public sealed record PushResult(int Accepted, int Failed, IReadOnlyList<string> InvalidTokens, string? Error = null, string? MessageId = null)
{
    public bool IsEmpty() => Accepted == 0 && Failed == 0 && InvalidTokens.Count == 0;
}

/// <summary>Public client-side identifiers (the content of google-services.json), never the service-account key.</summary>
public sealed record FirebaseClientConfigDto(string ApiKey, string ApplicationId, string MessagingSenderId, string ProjectId);
