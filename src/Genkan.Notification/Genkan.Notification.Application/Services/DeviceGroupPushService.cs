using Genkan.Notification.Application.Interfaces;
using Genkan.Notification.Domain.Dto;
using Genkan.Notification.Infrastructure.Interfaces;

namespace Genkan.Notification.Application.Services;

/// <summary>
/// Device-group notification: a named group of users maintained by this service.
/// FCM's native device groups are deprecated (decommissioned 2027-09-29), so a send resolves the members to their
/// registration tokens and uses multicast, which is what Firebase recommends as the replacement.
/// </summary>
public sealed class DeviceGroupPushService(IDeviceGroupRepository groups, IDeviceTokenRepository devices, IMulticastPushSender sender) : IDeviceGroupPushService
{
    public Task<IReadOnlyList<DeviceGroupDto>> ListAsync(CancellationToken ct) => groups.ListAsync(ct);

    public Task SetMembersAsync(string name, IReadOnlyList<Guid> userIds, CancellationToken ct) =>
        groups.SetMembersAsync(name, userIds, ct);

    public Task<bool> DeleteAsync(string name, CancellationToken ct) => groups.DeleteAsync(name, ct);

    public async Task<PushResult?> SendAsync(string name, PushMessage message, CancellationToken ct)
    {
        var members = await groups.GetMemberIdsAsync(name, ct);
        if (members.Count == 0) return null;

        var tokens = await devices.GetTokensForUsersAsync(members, ct);
        if (tokens.Count == 0) return PushResult.Empty(PushResult.NoDevice);

        var result = await sender.SendAsync(tokens, message, ct);
        if (result.InvalidTokens.Count > 0) await devices.RemoveTokensAsync(result.InvalidTokens, ct);
        return result;
    }
}
