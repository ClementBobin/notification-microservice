using Genkan.Lab.Domain.Dto;

namespace Genkan.Lab.Application.Interfaces;

public interface IMulticastPushService
{
    Task<PushDispatch> SendAsync(MulticastPushRequest request, CancellationToken ct);
}
