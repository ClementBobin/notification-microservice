using Genkan.Notification.Infrastructure.Data;
using Genkan.Notification.Infrastructure.Interfaces;
using Genkan.Notification.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Genkan.Notification.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    /// <summary>PostgreSQL context and the notification log repository (devices live in the lab service).</summary>
    public static IServiceCollection AddNotificationInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<NotificationDbContext>(o => o.UseNpgsql(config.GetConnectionString("Postgres")));
        services.AddScoped<INotificationRepository, NotificationRepository>();
        return services;
    }
}
