using Genkan.Lab.Infrastructure.Data;
using Genkan.Lab.Infrastructure.Interfaces;
using Genkan.Lab.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Genkan.Lab.Api.Extensions;

public static class DatabaseExtensions
{
    /// <summary>PostgreSQL context and the repositories built on it (tables are created by Genkan.Migrator).</summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<LabDbContext>(o => o.UseNpgsql(config.GetConnectionString("Postgres")));
        services.AddScoped<IDeviceTokenRepository, DeviceTokenRepository>();
        services.AddScoped<IDeviceGroupRepository, DeviceGroupRepository>();
        services.AddScoped<ITopicSubscriptionRepository, TopicSubscriptionRepository>();
        return services;
    }
}
