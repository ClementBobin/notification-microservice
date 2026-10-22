using Genkan.Lab.Application.Interfaces;
using Genkan.Lab.Domain.Dto;
using Genkan.Lab.Infrastructure.Interfaces;
using Genkan.Shared;
using Microsoft.Extensions.Logging;

namespace Genkan.Lab.Application.Services;

/// <summary>Individual notification: to one device token, or to the most recently used device of one user. The notification service sends it.</summary>
public sealed class IndividualPushService(IDeviceTokenRepository devices, IMessageBus bus, ILogger<IndividualPushService> log) : IIndividualPushService
{
    public async Task<PushDispatch> SendAsync(IndividualPushRequest request, CancellationToken ct)
    {
        var token = request.Token?.Trim();
        if (string.IsNullOrEmpty(token) && request.UserId is { } userId)
            token = await devices.GetLatestTokenAsync(userId, ct);
        if (string.IsNullOrEmpty(token)) return PushDispatch.Failed(PushDispatch.NoDevice);

        return await PushCommands.SendAsync(bus, log, PushKinds.Individual, [token], null, request.Message, ct);
    }
}
