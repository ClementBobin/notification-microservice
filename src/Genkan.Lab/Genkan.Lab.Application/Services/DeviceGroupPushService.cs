using Genkan.Lab.Application.Interfaces;
using Genkan.Lab.Domain.Dto;
using Genkan.Lab.Infrastructure.Interfaces;
using Genkan.Shared;
using Microsoft.Extensions.Logging;

namespace Genkan.Lab.Application.Services;

/// <summary>
/// Device-group notification: a named group of users maintained by this service.
/// FCM's native device groups are deprecated (decommissioned 2027-09-29), so a send resolves the members to their
/// registration tokens and the notification service uses multicast, which is what Firebase recommends as the replacement.
/// </summary>
public sealed class DeviceGroupPushService(IDeviceGroupRepository groups, IDeviceTokenRepository devices, IMessageBus bus, ILogger<DeviceGroupPushService> log) : IDeviceGroupPushService
{
    public Task<IReadOnlyList<DeviceGroupDto>> ListAsync(CancellationToken ct) => groups.ListAsync(ct);

    public Task SetMembersAsync(string name, IReadOnlyList<Guid> userIds, CancellationToken ct) =>
        groups.SetMembersAsync(name, userIds, ct);

    public Task<bool> DeleteAsync(string name, CancellationToken ct) => groups.DeleteAsync(name, ct);

    public async Task<PushDispatch?> SendAsync(string name, PushMessage message, CancellationToken ct)
    {
        var members = await groups.GetMemberIdsAsync(name, ct);
        if (members.Count == 0) return null;

        var tokens = await devices.GetTokensForUsersAsync(members, ct);
        if (tokens.Count == 0) return PushDispatch.Failed(PushDispatch.NoDevice);

        return await PushCommands.SendAsync(bus, log, PushKinds.Multicast, tokens, null, message, ct);
    }
}
