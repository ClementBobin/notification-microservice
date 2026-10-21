using Genkan.Notification.Infrastructure.Data;
using Genkan.Notification.Infrastructure.Interfaces;
using Genkan.Notification.Infrastructure.Repositories;
using Genkan.Shared;
using Microsoft.EntityFrameworkCore;

namespace Genkan.Notification.Api.Extensions;

public static class DatabaseExtensions
{
    /// <summary>PostgreSQL context and the repositories built on it.</summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<NotificationDbContext>(o => o.UseNpgsql(config.GetConnectionString("Postgres")));
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IDeviceTokenRepository, DeviceTokenRepository>();
        services.AddScoped<IDeviceGroupRepository, DeviceGroupRepository>();
        services.AddScoped<ITopicSubscriptionRepository, TopicSubscriptionRepository>();
        return services;
    }

    /// <summary>Creates the schema and tables (retries while PostgreSQL starts).</summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<NotificationDbContext>()
            .EnsureSchemaAsync(NotificationDbContext.Schema, "notifications", app.Logger);
    }
}
