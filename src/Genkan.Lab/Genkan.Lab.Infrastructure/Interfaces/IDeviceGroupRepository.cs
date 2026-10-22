using Genkan.Lab.Domain.Dto;

namespace Genkan.Lab.Infrastructure.Interfaces;

public interface IDeviceGroupRepository
{
    Task<IReadOnlyList<DeviceGroupDto>> ListAsync(CancellationToken ct);
    /// <summary>Empty when the group does not exist.</summary>
    Task<IReadOnlyList<Guid>> GetMemberIdsAsync(string name, CancellationToken ct);
    /// <summary>Creates the group or replaces its members. An empty list deletes the group.</summary>
    Task SetMembersAsync(string name, IReadOnlyCollection<Guid> userIds, CancellationToken ct);
    Task<bool> DeleteAsync(string name, CancellationToken ct);
}
