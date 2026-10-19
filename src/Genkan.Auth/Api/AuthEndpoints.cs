using System.Security.Claims;
using Genkan.Auth.Business;
using Genkan.Shared;

namespace Genkan.Auth.Api;

public sealed record LoginRequest(string Email, string Password);
public sealed record VerifyTwoFactorRequest(string ChallengeId, string Code);
public sealed record ConfirmTwoFactorRequest(string Code);
public sealed record CreateUserRequest(string Email, string DisplayName, string Password, string Role);

public static class AuthEndpoints
{
    private static string? Bearer(HttpRequest r)
    {
        var h = r.Headers.Authorization.ToString();
        return h.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? h[7..].Trim() : null;
    }

    private static IResult Authenticated(LoginResult r) =>
        Results.Ok(new { status = "authenticated", token = r.Token, expiresAt = r.ExpiresAt, user = r.User });

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/auth");

        // AUTH-01: session or 2FA challenge
        g.MapPost("/login", async (LoginRequest req, AuthService auth, CancellationToken ct) =>
        {
            var r = await auth.LoginAsync(req.Email, req.Password, ct);
            if (r is null) return Results.Json(new { error = "invalid_credentials" }, statusCode: 401);
            return r.TwoFactorRequired
                ? Results.Ok(new { status = "2fa_required", challengeId = r.ChallengeId, expiresInSeconds = 300 })
                : Authenticated(r);
        }).RequireRateLimiting("login");

        // AUTH-02 / AUTH-09
        g.MapPost("/2fa/verify", async (VerifyTwoFactorRequest req, AuthService auth, CancellationToken ct) =>
        {
            var r = await auth.VerifyTwoFactorAsync(req.ChallengeId, req.Code, ct);
            return r is null ? Results.Json(new { error = "invalid_code_or_challenge" }, statusCode: 401) : Authenticated(r);
        }).RequireRateLimiting("login");

        // AUTH-03: generate secret + otpauth URI, then confirm with a code
        g.MapPost("/2fa/setup", async (ClaimsPrincipal user, AuthService auth, CancellationToken ct) =>
        {
            var setup = await auth.BeginTwoFactorSetupAsync(Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!), ct);
            return setup is null
                ? Results.Conflict(new { error = "already_enabled_or_unknown_user" })
                : Results.Ok(new { secret = setup.Value.Secret, otpauthUri = setup.Value.OtpAuthUri });
        }).RequireAuthorization();

        g.MapPost("/2fa/confirm", async (ConfirmTwoFactorRequest req, ClaimsPrincipal user, AuthService auth, CancellationToken ct) =>
        {
            var ok = await auth.ConfirmTwoFactorAsync(Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!), req.Code, ct);
            return ok ? Results.Ok(new { twoFactorEnabled = true }) : Results.BadRequest(new { error = "invalid_code" });
        }).RequireAuthorization();

        // AUTH-05: called by the gateway on every protected request
        g.MapGet("/session", async (HttpRequest http, AuthService auth) =>
        {
            var token = Bearer(http);
            if (token is null) return Results.Unauthorized();
            var s = await auth.GetSessionAsync(token);
            return s is null ? Results.Unauthorized() : Results.Ok(s);
        });

        // AUTH-06
        g.MapPost("/logout", async (HttpRequest http, AuthService auth) =>
        {
            var token = Bearer(http);
            if (token is not null) await auth.LogoutAsync(token);
            return Results.NoContent();
        });

        // AUTH-07: admin only
        g.MapPost("/users", async (CreateUserRequest req, AuthService auth, CancellationToken ct) =>
        {
            var (user, error) = await auth.CreateUserAsync(req.Email, req.DisplayName, req.Password, req.Role, ct);
            return error is null
                ? Results.Created($"/auth/users/{user!.Id}", user)
                : Results.Json(new { error }, statusCode: error == "email_taken" ? 409 : 400);
        }).RequireAuthorization(p => p.RequireRole(Roles.Admin));

        return app;
    }
}
