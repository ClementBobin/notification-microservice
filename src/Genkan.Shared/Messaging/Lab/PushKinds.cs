namespace Genkan.Shared.Messaging.Lab;

/// <summary>The three ways a <see cref="PushCommand"/> can be addressed (its <see cref="PushCommand.Kind"/>).</summary>
public static class PushKinds
{
    /// <summary>One device: <see cref="PushCommand.Tokens"/> holds exactly one token.</summary>
    public const string Individual = "individual";
    /// <summary>A list of devices: <see cref="PushCommand.Tokens"/> holds the tokens (already resolved from users / device groups by lab).</summary>
    public const string Multicast = "multicast";
    /// <summary>Everybody subscribed to a topic: <see cref="PushCommand.Topic"/> holds the FCM topic.</summary>
    public const string Topic = "topic";
}
