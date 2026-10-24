using Genkan.Shared.Messaging;

namespace Genkan.Notification.Application.Interfaces;

/// <summary>Implemented by the application layer; the RabbitMQ project (Genkan.Notification.Rabbit) hands every received event to it.</summary>
public interface IEventHandler
{
    Task HandleAsync(EventEnvelope envelope, CancellationToken ct);
}
