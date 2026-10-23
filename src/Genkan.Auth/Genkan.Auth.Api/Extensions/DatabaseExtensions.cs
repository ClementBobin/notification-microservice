using Genkan.Auth.Application.Interfaces;
using Genkan.Auth.Infrastructure.Data;
using Genkan.Auth.Infrastructure.Interfaces;
using Genkan.Auth.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Genkan.Auth.Api.Extensions;

public static class DatabaseExtensions
{
    /// <summary>PostgreSQL context and the repositories built on it.</summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AuthDbContext>(o => o.UseNpgsql(config.GetConnectionString("Postgres")));
        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }
}
