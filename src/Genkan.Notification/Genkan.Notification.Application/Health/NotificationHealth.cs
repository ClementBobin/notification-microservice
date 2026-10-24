using Genkan.Notification.Infrastructure.Data;
using Genkan.Shared.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Genkan.Notification.Application.Health;

public static class NotificationHealth
{
    public static HealthProbe Database { get; } =
        new("database", async (sp, ct) => await sp.GetRequiredService<NotificationDbContext>().Database.CanConnectAsync(ct));
}
