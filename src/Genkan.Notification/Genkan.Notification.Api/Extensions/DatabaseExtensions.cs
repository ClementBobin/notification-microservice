namespace Genkan.Notification.Api.Extensions;

using Genkan.Notification.Api.Configuration;
using Genkan.Notification.Api.Interceptors;

using Genkan.Notification.Infrastructure.Configuration;
using Genkan.Notification.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<NotificationDbContext>(o => o.UseNpgsql(config.GetConnectionString("Postgres")));

        return services;
    }
}