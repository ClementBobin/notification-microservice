using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Genkan.Shared;

/// <summary>One named dependency check of a service (database, redis, rabbitmq...).</summary>
public sealed record HealthProbe(string Name, Func<IServiceProvider, CancellationToken, Task<bool>> Check);

public static class HealthEndpoints
{
    /// <summary>
    /// Maps GET /health and GET /{routePrefix}/health (the second one is routed through the gateway).
    /// 200 { status: "ok", service } when every probe passes, otherwise 503 with the result of each probe.
    /// </summary>
    public static IEndpointRouteBuilder MapServiceHealth(this IEndpointRouteBuilder app, string service, string routePrefix, params HealthProbe[] probes)
    {
        async Task<IResult> Handle(HttpContext ctx)
        {
            var results = new Dictionary<string, string>();
            foreach (var probe in probes)
            {
                try
                {
                    results[probe.Name] = await probe.Check(ctx.RequestServices, ctx.RequestAborted) ? "ok" : "down";
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    results[probe.Name] = ex.Message;
                }
            }

            return results.Values.All(v => v == "ok")
                ? Results.Ok(new { status = "ok", service })
                : Results.Json(new { status = "unhealthy", service, checks = results }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        app.MapGet("/health", Handle);
        app.MapGet($"/{routePrefix}/health", Handle);
        return app;
    }
}
