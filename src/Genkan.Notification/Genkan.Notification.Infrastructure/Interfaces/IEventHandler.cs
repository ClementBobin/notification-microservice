using Genkan.Shared.Messaging;

namespace Genkan.Notification.Infrastructure.Interfaces;

/// <summary>Implemented by the application layer; the RabbitMQ consumer hands every received event to it.</summary>
public interface IEventHandler
{
    Task HandleAsync(EventEnvelope envelope, CancellationToken ct);
}
