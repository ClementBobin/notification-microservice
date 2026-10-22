using Genkan.Lab.Domain.Dto;

namespace Genkan.Lab.Application.Interfaces;

public interface IIndividualPushService
{
    Task<PushDispatch> SendAsync(IndividualPushRequest request, CancellationToken ct);
}
