using Genkan.Shared.Entities;
using Genkan.Notification.Infrastructure.Data;
using Genkan.Notification.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Genkan.Notification.Infrastructure.Repositories;

public sealed class TopicSubscriptionRepository(NotificationDbContext db) : ITopicSubscriptionRepository
{
    public async Task AddAsync(string topic, IReadOnlyCollection<Guid> userIds, CancellationToken ct)
    {
        var ids = userIds.Distinct().ToList();
        var existing = await db.TopicSubscriptions
            .Where(s => s.Topic == topic && ids.Contains(s.UserId)).Select(s => s.UserId).ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        foreach (var id in ids.Except(existing))
            db.TopicSubscriptions.Add(new TopicSubscription { Id = Guid.NewGuid(), Topic = topic, UserId = id, CreatedAt = now });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.ChangeTracker.Clear(); // a concurrent request subscribed the same user first
        }
    }

    public Task RemoveAsync(string topic, IReadOnlyCollection<Guid> userIds, CancellationToken ct)
    {
        var ids = userIds.ToList();
        return db.TopicSubscriptions.Where(s => s.Topic == topic && ids.Contains(s.UserId)).ExecuteDeleteAsync(ct);
    }

    public async Task<IReadOnlyList<string>> GetTopicsAsync(Guid userId, CancellationToken ct) =>
        await db.TopicSubscriptions.AsNoTracking().Where(s => s.UserId == userId).Select(s => s.Topic).ToListAsync(ct);
}
