using Genkan.Shared.Auth;

namespace Genkan.Gateway;

public sealed record SessionDto(Guid UserId, string Email, string Role, string? DisplayName);

/// <summary>
/// Validates the bearer token with GET /auth/session on every non-public route, then forwards the identity
/// to the services as X-User-* headers. Client-supplied X-User-* headers are always stripped first.
/// </summary>
public sealed class TokenValidationMiddleware(RequestDelegate next, IHttpClientFactory httpFactory, ILogger<TokenValidationMiddleware> log)
{
    internal static readonly HashSet<string> PublicPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health", "/auth/login", "/auth/health", "/notifications/health",
    };

    public async Task InvokeAsync(HttpContext ctx)
    {
        ctx.Request.Headers.Remove(GatewayHeaders.UserId);
        ctx.Request.Headers.Remove(GatewayHeaders.UserRole);
        ctx.Request.Headers.Remove(GatewayHeaders.UserEmail);

        var path = (ctx.Request.Path.Value ?? "").TrimEnd('/');
        if (path.Length == 0 || PublicPaths.Contains(path))
        {
            await next(ctx);
            return;
        }

        var authorization = ctx.Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            await Reject(ctx, StatusCodes.Status401Unauthorized, "missing_token");
            return;
        }

        SessionDto? session;
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "/auth/session");
            req.Headers.TryAddWithoutValidation("Authorization", authorization);
            req.Headers.TryAddWithoutValidation(GatewayHeaders.CorrelationId, ctx.Request.Headers[GatewayHeaders.CorrelationId].ToString());
            using var res = await httpFactory.CreateClient("auth").SendAsync(req, ctx.RequestAborted);
            if (!res.IsSuccessStatusCode)
            {
                await Reject(ctx, StatusCodes.Status401Unauthorized, "invalid_or_expired_token");
                return;
            }
            session = await res.Content.ReadFromJsonAsync<SessionDto>(ctx.RequestAborted);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ctx.RequestAborted.IsCancellationRequested)
        {
            log.LogError(ex, "Auth service unreachable");
            await Reject(ctx, StatusCodes.Status503ServiceUnavailable, "auth_unavailable");
            return;
        }

        if (session is null)
        {
            await Reject(ctx, StatusCodes.Status401Unauthorized, "invalid_or_expired_token");
            return;
        }

        ctx.Request.Headers[GatewayHeaders.UserId] = session.UserId.ToString();
        ctx.Request.Headers[GatewayHeaders.UserRole] = session.Role;
        ctx.Request.Headers[GatewayHeaders.UserEmail] = session.Email;
        await next(ctx);
    }

    private static Task Reject(HttpContext ctx, int status, string error)
    {
        ctx.Response.StatusCode = status;
        return ctx.Response.WriteAsJsonAsync(new { error });
    }
}
