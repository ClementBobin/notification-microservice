using Genkan.Notification.Infrastructure.Data;
using Genkan.Notification.Infrastructure.Messaging;
using Genkan.Shared;
using Microsoft.EntityFrameworkCore;

namespace Genkan.Notification.Api.Extensions;

public static class HealthExtensions
{
    public static WebApplication MapNotificationHealth(this WebApplication app)
    {
        app.MapServiceHealth("notification", "notifications",
            new HealthProbe("database", async (sp, ct) => await sp.GetRequiredService<NotificationDbContext>().Database.CanConnectAsync(ct)),
            new HealthProbe("rabbitmq", async (sp, ct) => (await sp.GetRequiredService<RabbitMqConnection>().GetAsync(ct)).IsOpen));
        return app;
    }
}
