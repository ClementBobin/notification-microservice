using Genkan.Notification.Domain.Dto;

namespace Genkan.Notification.Application.Interfaces;

public interface IMulticastPushService
{
    Task<PushResult> SendAsync(MulticastPushRequest request, CancellationToken ct);
}
