using Genkan.Notification.Application.Interfaces;
using Genkan.Notification.Domain.Dto;
using Genkan.Shared.Auth;
using Genkan.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genkan.Notification.Api.Controllers;

[ApiController]
[Route("notifications")]
public sealed class NotificationsController(INotificationService svc) : BaseController
{
    /// <summary>The id of the current user.</summary>
    private Guid CurrentUserId => User.GetUserId();

    // Notifications of the current user (own + broadcast alerts)
    [HttpGet("me")]
    [Authorize]
    public Task<PagedResult<NotificationDto>> Me(string? status, int? page, int? pageSize, CancellationToken ct) =>
        svc.GetLogsAsync(CurrentUserId, true, null, status, page, pageSize, ct);

    // Admin: all logs
    [HttpGet("logs")]
    [Authorize(Roles = Roles.Admin)]
    public Task<PagedResult<NotificationDto>> Logs(Guid? userId, string? type, string? status, int? page, int? pageSize, CancellationToken ct) =>
        svc.GetLogsAsync(userId, false, type, status, page, pageSize, ct);

    // Admin: logs of one user (own + broadcast)
    [HttpGet("users/{userId:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public Task<PagedResult<NotificationDto>> UserLogs(Guid userId, string? status, int? page, int? pageSize, CancellationToken ct) =>
        svc.GetLogsAsync(userId, true, null, status, page, pageSize, ct);

    // Admin: alert to everyone
    [HttpPost("alerts")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreateAlert(CreateAlertRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.Message))
            return BadRequest(new { error = "title_and_message_required" });
        var eventId = await svc.CreateAlertAsync(req.Title.Trim(), req.Message.Trim(), CurrentUserId, ct);
        return Accepted("/notifications/logs?type=alert.broadcast", new { eventId });
    }
}
