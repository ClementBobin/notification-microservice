using Genkan.Notification.Application.Health;
using Genkan.Notification.Rabbit.Health;
using Genkan.Shared.Health;

namespace Genkan.Notification.Api.Extensions;

public static class HealthExtensions
{
    public static WebApplication MapNotificationHealth(this WebApplication app)
    {
        app.MapServiceHealth("notification", "notifications", NotificationHealth.Database, RabbitHealth.Probe);
        return app;
    }
}
