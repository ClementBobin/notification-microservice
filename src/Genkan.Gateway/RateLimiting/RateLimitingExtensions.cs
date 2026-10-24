using Genkan.Gateway.Configuration;
using Genkan.Gateway.Middleware;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace Genkan.Gateway.RateLimiting;

public static class RateLimitingExtensions
{
    /// <summary>Binds the "RateLimiting" section (validated at startup) and registers the bucket store chosen by RateLimiting:Store.</summary>
    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<RateLimitingOptions>()
            .Bind(config.GetSection("RateLimiting"))
            .Validate(o => o.IsValid(), "RateLimiting: every policy needs Capacity >= 1, RefillTokens > 0 and RefillPeriodSeconds > 0.")
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);

        services.AddMemoryCache();
        services.AddSingleton<IRateLimitStore, MemoryRateLimitStore>();
        return services;
    }

    /// <summary>Applies the named policy. Place it after TokenValidationMiddleware to limit per user, or before it to limit per IP.</summary>
    public static IApplicationBuilder UseGatewayRateLimit(this IApplicationBuilder app, string policy) =>
        app.UseMiddleware<RateLimitMiddleware>(policy);
}
