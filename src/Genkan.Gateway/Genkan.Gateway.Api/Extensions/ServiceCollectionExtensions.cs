using Genkan.Gateway.Application.Configuration;
using Genkan.Gateway.Application.Interfaces;
using Genkan.Gateway.Application.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Genkan.Gateway.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers everything the gateway's business logic needs: token validation, rate limiting and the combined Swagger document.</summary>
    public static IServiceCollection AddGatewayApplication(this IServiceCollection services, IConfiguration config) =>
        services
            .AddGatewaySessionValidation(config)
            .AddGatewayRateLimiting(config)
            .AddGatewaySwagger(config);

    /// <summary>Registers the auth client (Services:AuthUrl, 5 s timeout) and the validator that uses it.</summary>
    public static IServiceCollection AddGatewaySessionValidation(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient(AuthSessionValidator.HttpClientName, c =>
        {
            c.BaseAddress = new Uri(config["Services:AuthUrl"]!);
            c.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddSingleton<ISessionValidator, AuthSessionValidator>();
        return services;
    }

    /// <summary>Binds the "RateLimiting" section (validated at startup) and registers the bucket store.</summary>
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

    /// <summary>Binds the "Swagger" section and registers the aggregator. Nothing is exposed until the Api project maps /swagger with Enabled=true.</summary>
    public static IServiceCollection AddGatewaySwagger(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<SwaggerOptions>(config.GetSection(SwaggerOptions.Section));
        services.AddHttpClient(OpenApiAggregator.HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(5));
        services.AddSingleton<OpenApiAggregator>();
        return services;
    }
}
