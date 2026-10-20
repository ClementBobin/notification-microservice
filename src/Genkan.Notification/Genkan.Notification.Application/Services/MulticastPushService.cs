using Genkan.Notification.Application.Interfaces;
using Genkan.Notification.Domain.Dto;
using Genkan.Notification.Infrastructure.Interfaces;

namespace Genkan.Notification.Application.Services;

/// <summary>Multicast notification: an ad-hoc list of tokens and/or users (all their devices), de-duplicated.</summary>
public sealed class MulticastPushService(IMulticastPushSender sender, IDeviceTokenRepository devices) : IMulticastPushService
{
    public async Task<PushResult> SendAsync(MulticastPushRequest request, CancellationToken ct)
    {
        var tokens = new HashSet<string>();
        foreach (var t in request.Tokens ?? [])
            if (!string.IsNullOrWhiteSpace(t)) tokens.Add(t.Trim());
        if (request.UserIds is { Count: > 0 } userIds)
            foreach (var t in await devices.GetTokensForUsersAsync(userIds, ct)) tokens.Add(t);
        if (tokens.Count == 0) return PushResult.Empty(PushResult.NoDevice);

        var result = await sender.SendAsync(tokens.ToList(), request.Message, ct);
        if (result.InvalidTokens.Count > 0) await devices.RemoveTokensAsync(result.InvalidTokens, ct);
        return result;
    }
}
