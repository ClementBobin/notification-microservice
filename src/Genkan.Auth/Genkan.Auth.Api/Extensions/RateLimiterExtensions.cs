using System.Threading.RateLimiting;

namespace Genkan.Auth.Api.Extensions;

public static class RateLimiterExtensions
{
    /// <summary>
    /// Policy "login" (used by [EnableRateLimiting("login")]): 10 requests / minute / client IP.
    /// Behind the gateway the real client is the LAST X-Forwarded-For entry (appended by the gateway).
    /// </summary>
    public static IServiceCollection AddLoginRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy("login", ctx =>
            {
                var forwarded = ctx.Request.Headers["X-Forwarded-For"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var ip = forwarded.LastOrDefault() ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                });
            });
        });
        return services;
    }
}
