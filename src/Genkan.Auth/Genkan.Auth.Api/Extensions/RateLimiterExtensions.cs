namespace Api.Extensions;

using System.Text;

using Infrastructure.Configuration;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

public static class RateLimiterExtensions
{
    public static IServiceCollection AddRateLimiter(this IServiceCollection services,
        IConfiguration config)
    {
        // 10 requests / minute / client IP on the login endpoint.
        // Behind the gateway the real client is the LAST X-Forwarded-For entry (appended by the gateway).
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