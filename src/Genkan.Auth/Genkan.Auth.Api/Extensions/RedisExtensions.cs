using Genkan.Auth.Infrastructure.Data;
using Genkan.Auth.Infrastructure.Interfaces;
using StackExchange.Redis;

namespace Genkan.Auth.Api.Extensions;

public static class RedisExtensions
{
    /// <summary>Redis connection and the session store built on it.</summary>
    public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(config.GetConnectionString("Redis")!));
        services.AddSingleton<ISessionStore, RedisSessionStore>();
        return services;
    }
}
