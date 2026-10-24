using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Genkan.Shared.Auth;

public static class GatewayHeaderAuthExtensions
{
    public static IServiceCollection AddGatewayHeaderAuth(this IServiceCollection services)
    {
        services.AddAuthentication(GatewayHeaderAuthHandler.Scheme)
            .AddScheme<AuthenticationSchemeOptions, GatewayHeaderAuthHandler>(GatewayHeaderAuthHandler.Scheme, _ => { });
        services.AddAuthorization();
        return services;
    }
}
