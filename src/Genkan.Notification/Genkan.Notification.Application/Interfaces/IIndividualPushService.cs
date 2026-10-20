using Genkan.Notification.Domain.Dto;

namespace Genkan.Notification.Application.Interfaces;

public interface IIndividualPushService
{
    Task<PushResult> SendAsync(IndividualPushRequest request, CancellationToken ct);
}
