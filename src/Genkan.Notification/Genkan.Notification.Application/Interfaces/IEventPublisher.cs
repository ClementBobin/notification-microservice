using Genkan.Shared.Messaging;

namespace Genkan.Notification.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync(EventEnvelope envelope, CancellationToken ct);
}
