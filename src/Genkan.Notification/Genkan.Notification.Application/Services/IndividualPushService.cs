using Genkan.Notification.Application.Interfaces;
using Genkan.Notification.Domain.Dto;
using Genkan.Notification.Infrastructure.Interfaces;

namespace Genkan.Notification.Application.Services;

/// <summary>Individual notification: to one device token, or to the most recently used device of one user.</summary>
public sealed class IndividualPushService(IIndividualPushSender sender, IDeviceTokenRepository devices) : IIndividualPushService
{
    public async Task<PushResult> SendAsync(IndividualPushRequest request, CancellationToken ct)
    {
        var token = request.Token?.Trim();
        if (string.IsNullOrEmpty(token) && request.UserId is { } userId)
            token = await devices.GetLatestTokenAsync(userId, ct);
        if (string.IsNullOrEmpty(token)) return PushResult.Empty(PushResult.NoDevice);

        var result = await sender.SendAsync(token, request.Message, ct);
        if (result.InvalidTokens.Count > 0) await devices.RemoveTokensAsync(result.InvalidTokens, ct);
        return result;
    }
}
