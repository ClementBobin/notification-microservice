namespace Api.Extensions;

using System.Text;

using Infrastructure.Configuration;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

public static class AuthExtensions
{
    public static IServiceCollection AddJwtAuth(this IServiceCollection services,
        IConfiguration config)
    {

        return services;
    }
}