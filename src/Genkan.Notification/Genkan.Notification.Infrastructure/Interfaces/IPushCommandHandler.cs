using Genkan.Shared.Messaging.Lab;

namespace Genkan.Notification.Infrastructure.Interfaces;

/// <summary>Implemented by the application layer; the RabbitMQ consumer hands every command received from the lab service to it.</summary>
public interface IPushCommandHandler
{
    Task HandleAsync(PushCommand command, CancellationToken ct);
    Task HandleAsync(TopicSubscriptionCommand command, CancellationToken ct);
}
