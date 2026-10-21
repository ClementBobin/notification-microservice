using Genkan.Shared.Entities;
using Genkan.Notification.Domain.Dto;
using Genkan.Notification.Infrastructure.Data;
using Genkan.Notification.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Genkan.Notification.Infrastructure.Repositories;

public sealed class NotificationRepository(NotificationDbContext db) : INotificationRepository
{
    public async Task<bool> TryAddAsync(NotificationLog log, CancellationToken ct)
    {
        db.Notifications.Add(log);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.Entry(log).State = EntityState.Detached;
            return false;
        }
    }

    public Task UpdateStatusAsync(Guid id, string status, string? error, CancellationToken ct) =>
        db.Notifications.Where(n => n.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.Status, status).SetProperty(n => n.Error, error), ct);

    public async Task<(IReadOnlyList<NotificationLog> Items, int Total)> QueryAsync(NotificationFilter f, CancellationToken ct)
    {
        var q = db.Notifications.AsNoTracking().AsQueryable();
        if (f.UserId is { } uid)
            q = f.IncludeBroadcast ? q.Where(n => n.RecipientUserId == uid || n.IsBroadcast) : q.Where(n => n.RecipientUserId == uid);
        if (!string.IsNullOrWhiteSpace(f.Type)) q = q.Where(n => n.EventType == f.Type);
        if (!string.IsNullOrWhiteSpace(f.Status)) q = q.Where(n => n.Status == f.Status);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(n => n.CreatedAt)
            .Skip((f.Page - 1) * f.PageSize).Take(f.PageSize).ToListAsync(ct);
        return (items, total);
    }
}
