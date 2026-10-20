using Genkan.Auth.Application.Interfaces;
using Genkan.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Genkan.Auth.Api.Controllers;

public sealed record LoginRequest(string Email, string Password);
public sealed record CreateUserRequest(string Email, string? DisplayName, string Password, string Role);

[ApiController]
[Route("auth")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    private string? Bearer()
    {
        var h = Request.Headers.Authorization.ToString();
        return h.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? h[7..].Trim() : null;
    }

    // 10 requests / minute / client IP (policy "login" is configured in Program.cs)
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequest req, CancellationToken ct)
    {
        var r = await auth.LoginAsync(req.Email, req.Password, ct);
        return r is null
            ? Unauthorized(new { error = "invalid_credentials" })
            : Ok(new { status = "authenticated", token = r.Token, expiresAt = r.ExpiresAt, user = r.User });
    }

    // Called by the gateway on every protected request
    [HttpGet("session")]
    public async Task<IActionResult> Session()
    {
        var token = Bearer();
        if (token is null) return Unauthorized();
        var s = await auth.GetSessionAsync(token);
        return s is null ? Unauthorized() : Ok(s);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var token = Bearer();
        if (token is not null) await auth.LogoutAsync(token);
        return NoContent();
    }

    // Admin only
    [HttpPost("users")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreateUser(CreateUserRequest req, CancellationToken ct)
    {
        var (user, error) = await auth.CreateUserAsync(req.Email, req.DisplayName, req.Password, req.Role, ct);
        if (error is null) return Created($"/auth/users/{user!.Id}", user);
        return StatusCode(error == "email_taken" ? 409 : 400, new { error });
    }
}
