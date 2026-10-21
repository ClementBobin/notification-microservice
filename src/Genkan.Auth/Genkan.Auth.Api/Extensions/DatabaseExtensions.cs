using Genkan.Auth.Application.Interfaces;
using Genkan.Auth.Infrastructure.Data;
using Genkan.Auth.Infrastructure.Interfaces;
using Genkan.Auth.Infrastructure.Repositories;
using Genkan.Shared;
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

    /// <summary>Creates the schema and tables (retries while PostgreSQL starts), then seeds the first administrator.</summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AuthDbContext>()
            .EnsureSchemaAsync(AuthDbContext.Schema, "users", app.Logger);
        await scope.ServiceProvider.GetRequiredService<IAdminSeeder>().SeedAsync(default);
    }
}
