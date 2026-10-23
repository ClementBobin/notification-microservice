using Genkan.Auth.Application.Interfaces;
using Genkan.Auth.Application.Services;

namespace Genkan.Auth.Api.Extensions;

public static class AppServicesExtensions
{
    /// <summary>Application layer: services and their options (infrastructure is registered by AddDatabase / AddRedis).</summary>
    public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<AuthOptions>(config.GetSection("Auth"));
        services.Configure<SeedOptions>(config.GetSection("Seed"));
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminSeeder, AdminSeeder>();
        services.AddScoped<IUserService, UserService>();
        return services;
    }
}
