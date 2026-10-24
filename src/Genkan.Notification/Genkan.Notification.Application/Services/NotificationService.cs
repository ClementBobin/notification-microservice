using System.Text.Json;
using Genkan.Notification.Application.Interfaces;
using Genkan.Notification.Domain.Dto;
using Genkan.Notification.Infrastructure.Interfaces;
using Genkan.Shared.Messaging;
using Genkan.Shared.Entities;

namespace Genkan.Notification.Application.Services;

public sealed class NotificationService(
    INotificationRepository repo) : INotificationService
{
    private static NotificationDto ToDto(NotificationLog n) => new(
        n.Id, n.EventId, n.EventType, n.IsBroadcast ? null : n.RecipientUserId, n.IsBroadcast,
        n.Title, n.Body, n.Status, n.Error, n.OccurredAt, n.CreatedAt);

    public async Task<PagedResult<NotificationDto>> GetLogsAsync(Guid? userId, bool includeBroadcast, string? type, string? status, int? page, int? pageSize, CancellationToken ct)
    {
        var p = Math.Max(page ?? 1, 1);
        var size = Math.Clamp(pageSize ?? 25, 1, 100);
        var (items, total) = await repo.QueryAsync(new NotificationFilter(userId, includeBroadcast, type, status, p, size), ct);
        return new PagedResult<NotificationDto>(items.Select(ToDto).ToList(), total, p, size);
    }
}
