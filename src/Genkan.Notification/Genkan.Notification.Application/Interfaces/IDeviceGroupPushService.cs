using Genkan.Notification.Domain.Dto;

namespace Genkan.Notification.Application.Interfaces;

public interface IDeviceGroupPushService
{
    Task<IReadOnlyList<DeviceGroupDto>> ListAsync(CancellationToken ct);
    /// <summary>Creates the group or replaces its members. An empty list deletes the group.</summary>
    Task SetMembersAsync(string name, IReadOnlyList<Guid> userIds, CancellationToken ct);
    Task<bool> DeleteAsync(string name, CancellationToken ct);
    /// <summary>Returns null when the group does not exist.</summary>
    Task<PushResult?> SendAsync(string name, PushMessage message, CancellationToken ct);
}
