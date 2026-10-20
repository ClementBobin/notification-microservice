namespace Api.Extensions;

using Api.Configuration;
using Api.Interceptors;

using Infrastructure.Configuration;
using Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public static class RedisExtensions
{
    public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(config.GetConnectionString("Redis")!));

        services.AddSingleton<ISessionStore, RedisSessionStore>();

        return services;
    }
}