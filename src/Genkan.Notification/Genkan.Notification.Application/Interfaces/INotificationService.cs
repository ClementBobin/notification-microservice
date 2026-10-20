using Genkan.Notification.Domain.Dto;

namespace Genkan.Notification.Application.Interfaces;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetLogsAsync(Guid? userId, bool includeBroadcast, string? type, string? status, int? page, int? pageSize, CancellationToken ct);
    Task<Guid> CreateAlertAsync(string title, string message, Guid createdBy, CancellationToken ct);
    Task RegisterDeviceAsync(Guid userId, string token, string platform, CancellationToken ct);
    Task<bool> UnregisterDeviceAsync(Guid userId, string token, CancellationToken ct);
}
