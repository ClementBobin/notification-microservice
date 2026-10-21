using Genkan.Notification.Domain.Dto;
using Genkan.Shared.Entities;

namespace Genkan.Notification.Infrastructure.Interfaces;

public interface INotificationRepository
{
    /// <summary>Inserts the row; returns false if (EventId, RecipientUserId) already exists.</summary>
    Task<bool> TryAddAsync(NotificationLog log, CancellationToken ct);
    Task UpdateStatusAsync(Guid id, string status, string? error, CancellationToken ct);
    Task<(IReadOnlyList<NotificationLog> Items, int Total)> QueryAsync(NotificationFilter filter, CancellationToken ct);
}
