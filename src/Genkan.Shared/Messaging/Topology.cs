namespace Genkan.Shared.Messaging;

public static class Topology
{
    public const string Exchange = "workplace.events";
    public const string DeadLetterExchange = "workplace.events.dlx";
    public const string NotificationQueue = "notification-service";
    public const string NotificationDeadQueue = "notification-service.dead";
    public static readonly string[] NotificationBindings = ["booking.#", "access.#", "visitor.#", "alert.#"];
}
