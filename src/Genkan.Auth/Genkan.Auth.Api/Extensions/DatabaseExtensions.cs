namespace Api.Extensions;

using Api.Configuration;
using Api.Interceptors;

using Infrastructure.Configuration;
using Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AuthDbContext>(o => o.UseNpgsql(config.GetConnectionString("Postgres")));

        return services;
    }
}