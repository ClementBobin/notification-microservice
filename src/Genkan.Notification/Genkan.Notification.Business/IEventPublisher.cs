using Genkan.Shared;

namespace Genkan.Notification.Business;

/// <summary>Port implemented by the messaging layer (RabbitMQ); lives in Business so Business never references the broker.</summary>
public interface IEventPublisher
{
    Task PublishAsync(EventEnvelope envelope, CancellationToken ct);
}
