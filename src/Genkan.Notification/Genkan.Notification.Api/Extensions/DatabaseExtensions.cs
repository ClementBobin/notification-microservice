using Genkan.Notification.Infrastructure.Data;
using Genkan.Notification.Infrastructure.Interfaces;
using Genkan.Notification.Infrastructure.Repositories;
using Genkan.Shared;
using Microsoft.EntityFrameworkCore;

namespace Genkan.Notification.Api.Extensions;

public static class DatabaseExtensions
{
    /// <summary>PostgreSQL context and the notification log repository (devices live in the lab service, see AddRabbitServices).</summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<NotificationDbContext>(o => o.UseNpgsql(config.GetConnectionString("Postgres")));
        services.AddScoped<INotificationRepository, NotificationRepository>();
        return services;
    }
}
