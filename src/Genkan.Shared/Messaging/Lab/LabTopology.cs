using RabbitMQ.Client;

namespace Genkan.Shared.Messaging.Lab;

/// <summary>
/// The RabbitMQ "wiring" between the <b>lab</b> service (owns devices, groups and topic subscriptions) and the
/// <b>notification</b> service (talks to Firebase Cloud Messaging, FCM). Everything is declared here so both services agree.
/// </summary>
/// <remarks>
/// <para><b>RabbitMQ in one minute</b></para>
/// <list type="bullet">
/// <item><description><b>Message</b>: a JSON body (<see cref="Genkan.Shared.Messaging.EventJson.Options"/>) plus a <b>routing key</b>,
///   a label such as <c>push.send</c> that tells what the message is.</description></item>
/// <item><description><b>Exchange</b>: the sorting office. A service never writes into a queue: it hands (message + routing key) to an
///   exchange. <see cref="Exchange"/> is a <i>topic</i> exchange, it sorts by routing key.</description></item>
/// <item><description><b>Queue</b>: a mailbox that keeps messages until a consumer reads and acknowledges them.</description></item>
/// <item><description><b>Binding</b>: a rule "routing key K goes to queue Q". A message matching no binding is silently dropped.</description></item>
/// <item><description><b>Dead-letter queue</b> (<see cref="DeadQueue"/>): where a message lands after it failed twice
///   (first delivery + one retry), so nothing is lost and it can be inspected in the RabbitMQ UI.</description></item>
/// </list>
/// <para><b>The six messages</b></para>
/// <code>
/// routing key              body                       direction                        what the receiver does
/// ----------------------   ------------------------   ------------------------------   ------------------------------------------------
/// device.registered        DeviceRegistered           lab -> EVERY notification inst.  adds / moves the token in its in-memory cache
/// device.removed           DevicesRemoved             lab -> EVERY notification inst.  removes the tokens from its in-memory cache
/// push.send                PushCommand                lab -> ONE notification inst.    sends the push through FCM (individual/multicast/topic)
/// push.topic-subscription  TopicSubscriptionCommand   lab -> ONE notification inst.    subscribes / unsubscribes tokens to an FCM topic
/// snapshot.request         (empty, RPC)               notification -> lab              lab replies with a DeviceSnapshot (see below)
/// device.invalid           DevicesInvalid             notification -> lab              lab deletes the dead tokens, then sends device.removed
/// </code>
/// <para><b>Why "EVERY" vs "ONE"</b></para>
/// <para>
/// <i>Events</i> (<c>device.*</c>, something happened) must reach every notification instance, because each one keeps its own cache:
/// each instance creates a private queue and binds it to those keys, so every instance gets its own copy.
/// <i>Commands</i> (<c>push.*</c>, "please do this") must run once: all instances read the same shared queue
/// (<see cref="PushCommandQueue"/>) and RabbitMQ gives each message to only one of them, otherwise a push would be sent several times.
/// </para>
/// <para><b>Typical flows</b></para>
/// <list type="number">
/// <item><description><b>A phone logs in</b>: the app registers its FCM token on lab. Lab saves it, sends <c>device.registered</c>
///   (notification caches the token) and one <c>push.topic-subscription</c> per topic the device must join (e.g. <see cref="PushTopics.Broadcast"/>).</description></item>
/// <item><description><b>An admin sends a push</b> through the lab API: lab turns users / groups into device tokens and sends
///   <c>push.send</c>; one notification instance sends it to FCM.</description></item>
/// <item><description><b>FCM says a token is dead</b>: notification sends <c>device.invalid</c>; lab deletes the tokens and announces
///   <c>device.removed</c>, so every instance forgets them.</description></item>
/// <item><description><b>A notification instance starts</b> (and then every few minutes as a safety net): it sends <c>snapshot.request</c>
///   and lab answers with the full device list. This is an RPC: the request carries <c>ReplyTo</c> (a private queue of the asker)
///   and <c>CorrelationId</c> (to match the answer to the question), and lab publishes the reply straight to that queue.</description></item>
/// </list>
/// <para>
/// Both services call <see cref="DeclareAsync"/> before publishing or consuming, so the declarations always match.
/// Lab owns the data: notification never reads those tables, it only executes commands and mirrors the tokens it is told about.
/// </para>
/// </remarks>
public static class LabTopology
{
    /// <summary>The topic exchange every lab &lt;-&gt; notification message goes through.</summary>
    public const string Exchange = "genkan.lab";
    /// <summary>Where rejected messages are sent (a fanout exchange, it forwards everything to <see cref="DeadQueue"/>).</summary>
    public const string DeadLetterExchange = "genkan.lab.dlx";
    /// <summary>Parking queue for messages that failed twice. Nobody consumes it: look at it in the RabbitMQ UI.</summary>
    public const string DeadQueue = "genkan.lab.dead";

    /// <summary>Mailbox of the <b>lab</b> service: receives <see cref="SnapshotRequestKey"/> and <see cref="DeviceInvalidKey"/> (see <see cref="LabBindings"/>).</summary>
    public const string LabQueue = "lab-service";
    /// <summary>Mailbox shared by all <b>notification</b> instances: receives <see cref="PushSendKey"/> and <see cref="TopicSubscriptionKey"/>; each message goes to one instance.</summary>
    public const string PushCommandQueue = "notification-push-commands";

    // ---- Events: lab -> every notification instance (each one binds its own private queue to these keys) ----

    /// <summary>Body: <see cref="DeviceRegistered"/>. A device token was registered, or moved to another user.</summary>
    public const string DeviceRegisteredKey = "device.registered";
    /// <summary>Body: <see cref="DevicesRemoved"/>. These device tokens no longer exist (logout, or cleanup after <see cref="DeviceInvalidKey"/>).</summary>
    public const string DeviceRemovedKey = "device.removed";

    // ---- Commands: lab -> one notification instance (read from PushCommandQueue) ----

    /// <summary>Body: <see cref="PushCommand"/>. Send this push notification through FCM.</summary>
    public const string PushSendKey = "push.send";
    /// <summary>Body: <see cref="TopicSubscriptionCommand"/>. Subscribe or unsubscribe these tokens to an FCM topic.</summary>
    public const string TopicSubscriptionKey = "push.topic-subscription";

    // ---- Messages: notification -> lab (read from LabQueue) ----

    /// <summary>RPC request, empty body. "Send me every device you know": lab answers with a <see cref="DeviceSnapshot"/> on the <c>ReplyTo</c> queue.</summary>
    public const string SnapshotRequestKey = "snapshot.request";
    /// <summary>Body: <see cref="DevicesInvalid"/>. FCM refused these tokens as unregistered: lab must delete them.</summary>
    public const string DeviceInvalidKey = "device.invalid";

    /// <summary>Bindings of <see cref="LabQueue"/>: what lab receives.</summary>
    public static readonly string[] LabBindings = [SnapshotRequestKey, DeviceInvalidKey];
    /// <summary>Bindings of <see cref="PushCommandQueue"/>: what notification must execute.</summary>
    public static readonly string[] PushCommandBindings = [PushSendKey, TopicSubscriptionKey];

    /// <summary>Idempotent: exchange, dead-letter exchange + queue, and the two durable queues with their bindings.</summary>
    public static async Task DeclareAsync(IChannel ch, CancellationToken ct)
    {
        await ch.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
        await ch.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Fanout, durable: true, cancellationToken: ct);

        await ch.QueueDeclareAsync(DeadQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await ch.QueueBindAsync(DeadQueue, DeadLetterExchange, "", cancellationToken: ct);

        var args = new Dictionary<string, object?> { ["x-dead-letter-exchange"] = DeadLetterExchange };
        await DeclareBoundQueueAsync(ch, LabQueue, LabBindings, args, ct);
        await DeclareBoundQueueAsync(ch, PushCommandQueue, PushCommandBindings, args, ct);
    }

    private static async Task DeclareBoundQueueAsync(IChannel ch, string queue, string[] bindings, Dictionary<string, object?> args, CancellationToken ct)
    {
        await ch.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, arguments: args, cancellationToken: ct);
        foreach (var key in bindings)
            await ch.QueueBindAsync(queue, Exchange, key, cancellationToken: ct);
    }
}
