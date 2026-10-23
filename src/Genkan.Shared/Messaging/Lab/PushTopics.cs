namespace Genkan.Shared.Messaging.Lab;

/// <summary>Names of the FCM topics the platform uses.</summary>
public static class PushTopics
{
    /// <summary>
    /// Every registered device is subscribed to it by lab when its token is registered, so a push to everyone is one topic send.
    /// FCM creates a topic when its first device subscribes: nothing has to be created beforehand.
    /// </summary>
    public const string Broadcast = "broadcast";
}
