using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Genkan.Shared;

public static class CorrelationIdExtensions
{
    /// <summary>Ensures an X-Correlation-Id exists on the request, echoes it, and adds it to the log scope.</summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.Use(async (ctx, next) =>
        {
            var id = ctx.Request.Headers[GatewayHeaders.CorrelationId].ToString();
            if (string.IsNullOrWhiteSpace(id))
            {
                id = Guid.NewGuid().ToString("N");
                ctx.Request.Headers[GatewayHeaders.CorrelationId] = id;
            }
            ctx.Response.OnStarting(() =>
            {
                ctx.Response.Headers[GatewayHeaders.CorrelationId] = id;
                return Task.CompletedTask;
            });
            var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Correlation");
            using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id }))
                await next();
        });
}
