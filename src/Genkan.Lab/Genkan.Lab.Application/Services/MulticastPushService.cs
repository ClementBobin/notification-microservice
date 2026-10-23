using Genkan.Lab.Application.Interfaces;
using Genkan.Lab.Domain.Dto;
using Genkan.Lab.Infrastructure.Interfaces;
using Genkan.Shared.Messaging.Lab;
using Microsoft.Extensions.Logging;

namespace Genkan.Lab.Application.Services;

/// <summary>Multicast notification: an ad-hoc list of tokens and/or users (all their devices), de-duplicated. The notification service sends it.</summary>
public sealed class MulticastPushService(IDeviceTokenRepository devices, IMessageBus bus, ILogger<MulticastPushService> log) : IMulticastPushService
{
    public async Task<PushDispatch> SendAsync(MulticastPushRequest request, CancellationToken ct)
    {
        var tokens = new HashSet<string>();
        foreach (var t in request.Tokens ?? [])
            if (!string.IsNullOrWhiteSpace(t)) tokens.Add(t.Trim());
        if (request.UserIds is { Count: > 0 } userIds)
            foreach (var t in await devices.GetTokensForUsersAsync(userIds, ct)) tokens.Add(t);
        if (tokens.Count == 0) return PushDispatch.Failed(PushDispatch.NoDevice);

        return await PushCommands.SendAsync(bus, log, PushKinds.Multicast, tokens.ToList(), null, request.Message, ct);
    }
}
