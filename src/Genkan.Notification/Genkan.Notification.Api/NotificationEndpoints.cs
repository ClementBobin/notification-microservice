using System.Security.Claims;
using Genkan.Notification.Business;
using Genkan.Shared;

namespace Genkan.Notification.Api;

public sealed record CreateAlertRequest(string Title, string Message);

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/notifications");

        // Current user's notifications (own + broadcast alerts)
        g.MapGet("/me", (ClaimsPrincipal user, NotificationService svc, string? status, int? page, int? pageSize, CancellationToken ct) =>
            svc.GetLogsAsync(Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!), true, null, status, page, pageSize, ct))
            .RequireAuthorization();

        // Admin: all logs
        g.MapGet("/logs", (NotificationService svc, Guid? userId, string? type, string? status, int? page, int? pageSize, CancellationToken ct) =>
            svc.GetLogsAsync(userId, false, type, status, page, pageSize, ct))
            .RequireAuthorization(p => p.RequireRole(Roles.Admin));

        // Admin: logs of one user (own + broadcast)
        g.MapGet("/users/{userId:guid}", (Guid userId, NotificationService svc, string? status, int? page, int? pageSize, CancellationToken ct) =>
            svc.GetLogsAsync(userId, true, null, status, page, pageSize, ct))
            .RequireAuthorization(p => p.RequireRole(Roles.Admin));

        // Admin: alert to everyone
        g.MapPost("/alerts", async (CreateAlertRequest req, ClaimsPrincipal user, NotificationService svc, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.Message))
                return Results.BadRequest(new { error = "title_and_message_required" });
            var eventId = await svc.CreateAlertAsync(req.Title.Trim(), req.Message.Trim(), Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!), ct);
            return Results.Accepted($"/notifications/logs?type=alert.broadcast", new { eventId });
        }).RequireAuthorization(p => p.RequireRole(Roles.Admin));

        return app;
    }
}
