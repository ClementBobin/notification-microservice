namespace Genkan.Shared.Messaging;

/// <summary>
/// RabbitMQ wiring for the <b>workplace events</b> (<see cref="EventEnvelope"/>) consumed by the notification service.
/// For lab &lt;-&gt; notification messages see <see cref="Lab.LabTopology"/>, which also explains the RabbitMQ vocabulary.
/// </summary>
/// <remarks>
/// A service that sees something happen publishes an <see cref="EventEnvelope"/> to <see cref="Exchange"/> with the event type as routing key
/// (<c>booking.created</c>, <c>access.denied</c>...). The exchange copies it into every queue with a matching binding:
/// <code>
/// booking.created --\
/// access.denied   ---+--> exchange "workplace.events" --(bindings)--> queue "notification-service" --> notification consumer
/// visitor.checked-in-/                                                       | failed twice
/// alert.broadcast                                                            v
///                                                         exchange "workplace.events.dlx" --> queue "notification-service.dead"
/// </code>
/// In a binding, <c>#</c> means "any suffix": <c>booking.#</c> matches <c>booking.created</c> and <c>booking.cancelled</c>.
/// A new family of events (say <c>door.#</c>) is added by adding its pattern to <see cref="NotificationBindings"/>
/// and a template to the notification templates; an event type with no template is logged and ignored.
/// </remarks>
public static class Topology
{
    /// <summary>The topic exchange where every workplace event is published.</summary>
    public const string Exchange = "workplace.events";
    /// <summary>Where events that failed twice are sent; it forwards them to <see cref="NotificationDeadQueue"/>.</summary>
    public const string DeadLetterExchange = "workplace.events.dlx";
    /// <summary>Durable mailbox of the notification service: events wait here while the service is down.</summary>
    public const string NotificationQueue = "notification-service";
    /// <summary>Parking queue for events the notification service could not process. Inspect it in the RabbitMQ UI.</summary>
    public const string NotificationDeadQueue = "notification-service.dead";
    /// <summary>Routing key patterns copied into <see cref="NotificationQueue"/>: room bookings, door access, visitors and alerts.</summary>
    public static readonly string[] NotificationBindings = ["booking.#", "access.#", "visitor.#", "alert.#"];
}
