using Genkan.Shared;

namespace Genkan.Notification.Infrastructure.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync(EventEnvelope envelope, CancellationToken ct);
}
