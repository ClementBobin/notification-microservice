using Genkan.Shared.Entities;
using Genkan.Notification.Domain.Dto;
using Genkan.Notification.Infrastructure.Data;
using Genkan.Notification.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Genkan.Notification.Infrastructure.Repositories;

public sealed class DeviceGroupRepository(NotificationDbContext db) : IDeviceGroupRepository
{
    public async Task<IReadOnlyList<DeviceGroupDto>> ListAsync(CancellationToken ct)
    {
        var rows = await db.DeviceGroupMembers.AsNoTracking()
            .GroupBy(m => m.GroupName)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
        return rows.Select(r => new DeviceGroupDto(r.Name, r.Count)).ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetMemberIdsAsync(string name, CancellationToken ct) =>
        await db.DeviceGroupMembers.AsNoTracking().Where(m => m.GroupName == name).Select(m => m.UserId).ToListAsync(ct);

    public async Task SetMembersAsync(string name, IReadOnlyCollection<Guid> userIds, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.DeviceGroupMembers.Where(m => m.GroupName == name).ExecuteDeleteAsync(ct);

        var now = DateTimeOffset.UtcNow;
        db.DeviceGroupMembers.AddRange(userIds.Distinct().Select(id => new DeviceGroupMember
        {
            Id = Guid.NewGuid(), GroupName = name, UserId = id, CreatedAt = now,
        }));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<bool> DeleteAsync(string name, CancellationToken ct) =>
        await db.DeviceGroupMembers.Where(m => m.GroupName == name).ExecuteDeleteAsync(ct) > 0;
}
