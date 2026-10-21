using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Genkan.Shared;

public static class ServiceDefaultsExtensions
{
    /// <summary>JSON console logging with scopes, so the correlation id appears on every log line.</summary>
    public static ILoggingBuilder AddGenkanLogging(this ILoggingBuilder logging) =>
        logging.AddJsonConsole(o => o.IncludeScopes = true);

    /// <summary>For the API services behind the gateway: authentication from the gateway headers + controllers.</summary>
    public static IServiceCollection AddGenkanApi(this IServiceCollection services)
    {
        services.AddGatewayHeaderAuth();
        services.AddControllers();
        return services;
    }

    /// <summary>Correlation id, then authentication and authorization. Add middleware that must run later (rate limiting...) after it.</summary>
    public static IApplicationBuilder UseGenkanApi(this IApplicationBuilder app) =>
        app.UseCorrelationId().UseAuthentication().UseAuthorization();
}
