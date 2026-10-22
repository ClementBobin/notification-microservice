using Genkan.Notification.Domain.Dto;

namespace Genkan.Notification.Application.Interfaces;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetLogsAsync(Guid? userId, bool includeBroadcast, string? type, string? status, int? page, int? pageSize, CancellationToken ct);
    Task<Guid> CreateAlertAsync(string title, string message, Guid createdBy, CancellationToken ct);
}
