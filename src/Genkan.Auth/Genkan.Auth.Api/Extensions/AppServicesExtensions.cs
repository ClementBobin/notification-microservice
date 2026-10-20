namespace Genkan.Auth.Api.Extensions;

using Genkan.Auth.Application.Interfaces;
using Genkan.Auth.Application.Services;
using Genkan.Auth.Infrastructure.Interfaces;
using Genkan.Auth.Infrastructure.Repositories;

public static class AppServicesExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services,
        IConfiguration config)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}