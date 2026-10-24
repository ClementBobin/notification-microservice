using Genkan.Notification.Infrastructure.Data;
using Genkan.Notification.Rabbit;
using Genkan.Shared.Health;

namespace Genkan.Notification.Api.Extensions;

public static class HealthExtensions
{
    public static WebApplication MapNotificationHealth(this WebApplication app)
    {
        app.MapServiceHealth("notification", "notifications", ProbeDatabase, ProbeRabbit);
        return app;
    }

    public static HealthProbe ProbeDatabase { get; } =
        new("database", async (sp, ct) => await sp.GetRequiredService<NotificationDbContext>().Database.CanConnectAsync(ct));

    public static HealthProbe ProbeRabbit { get; } =
        new("rabbitmq", async (sp, ct) => (await sp.GetRequiredService<RabbitMqConnection>().GetAsync(ct)).IsOpen);
}
