using FirebaseAdmin.Messaging;
using Genkan.Notification.Domain.Dto;

namespace Genkan.Notification.Firebase.Push;

/// <summary>Builds the FCM message objects from the common <see cref="PushMessage"/> (notification + data, high priority on Android).</summary>
internal static class FcmMessageBuilder
{
    // Fully qualified: inside Genkan.Notification.* the simple name "Notification" is our namespace.
    private static FirebaseAdmin.Messaging.Notification ToNotification(PushMessage m) => new() { Title = m.Title, Body = m.Body };

    private static Dictionary<string, string>? ToData(PushMessage m) => m.Data is null ? null : new Dictionary<string, string>(m.Data);

    public static Message ForTopic(string topic, PushMessage m) =>
        new() { Topic = topic, Notification = ToNotification(m), Data = ToData(m) };

    [Obsolete("Fid token in the future")]
    public static MulticastMessage ForTokens(IReadOnlyList<string> tokens, PushMessage m) =>
        new() { Tokens = tokens, Notification = ToNotification(m), Data = ToData(m) };
}
