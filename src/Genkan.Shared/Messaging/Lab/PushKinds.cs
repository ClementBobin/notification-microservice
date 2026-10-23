namespace Genkan.Shared.Messaging.Lab;

public static class PushKinds
{
    /// <summary><see cref="PushCommand.Tokens"/> holds exactly one token.</summary>
    public const string Individual = "individual";
    /// <summary><see cref="PushCommand.Tokens"/> holds the tokens (already resolved from users / device groups by lab).</summary>
    public const string Multicast = "multicast";
    /// <summary><see cref="PushCommand.Topic"/> holds the FCM topic.</summary>
    public const string Topic = "topic";
}
