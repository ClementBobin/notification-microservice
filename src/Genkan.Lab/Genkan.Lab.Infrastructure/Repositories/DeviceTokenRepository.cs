using Genkan.Lab.Infrastructure.Data;
using Genkan.Lab.Infrastructure.Interfaces;
using Genkan.Shared;
using Genkan.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Genkan.Lab.Infrastructure.Repositories;

public sealed class DeviceTokenRepository(LabDbContext db) : IDeviceTokenRepository
{
    public async Task UpsertAsync(Guid userId, string token, string platform, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        if (await Touch(userId, token, platform, now, ct) > 0) return;

        db.DeviceTokens.Add(new DeviceToken
        {
            Id = Guid.NewGuid(), UserId = userId, Token = token, Platform = platform, CreatedAt = now, LastSeenAt = now,
        });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // A concurrent request inserted the same token first: update that row instead.
            db.ChangeTracker.Clear();
            await Touch(userId, token, platform, now, ct);
        }
    }

    private Task<int> Touch(Guid userId, string token, string platform, DateTimeOffset now, CancellationToken ct) =>
        db.DeviceTokens.Where(d => d.Token == token)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.UserId, userId)
                .SetProperty(d => d.Platform, platform)
                .SetProperty(d => d.LastSeenAt, now), ct);

    public async Task<bool> RemoveAsync(Guid userId, string token, CancellationToken ct) =>
        await db.DeviceTokens.Where(d => d.UserId == userId && d.Token == token).ExecuteDeleteAsync(ct) > 0;

    public async Task<IReadOnlyList<DeviceSnapshotItem>> GetAllAsync(CancellationToken ct) =>
        await db.DeviceTokens.AsNoTracking()
            .Select(d => new DeviceSnapshotItem(d.UserId, d.Token, d.Platform))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<string>> GetTokensForUsersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct)
    {
        var ids = userIds.ToList();
        return await db.DeviceTokens.AsNoTracking().Where(d => ids.Contains(d.UserId)).Select(d => d.Token).ToListAsync(ct);
    }

    public Task<string?> GetLatestTokenAsync(Guid userId, CancellationToken ct) =>
        db.DeviceTokens.AsNoTracking().Where(d => d.UserId == userId)
            .OrderByDescending(d => d.LastSeenAt).Select(d => d.Token).FirstOrDefaultAsync(ct);

    public Task RemoveTokensAsync(IReadOnlyCollection<string> tokens, CancellationToken ct)
    {
        var list = tokens.ToList();
        return db.DeviceTokens.Where(d => list.Contains(d.Token)).ExecuteDeleteAsync(ct);
    }
}
