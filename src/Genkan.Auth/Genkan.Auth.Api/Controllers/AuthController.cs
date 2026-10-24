using Genkan.Auth.Application.Interfaces;
using Genkan.Auth.Domain.Dto;
using Genkan.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Genkan.Auth.Api.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class AuthController(IAuthService auth) : BaseController
{
    private string? Bearer()
    {
        var h = Request.Headers.Authorization.ToString();
        return h.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? h[7..].Trim() : null;
    }

    // 10 requests / minute / client IP (policy "login" is configured in Program.cs)
    [HttpPost("[action]")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequest req, CancellationToken ct)
    {
        var r = await auth.LoginAsync(req.Email, req.Password, ct);
        return r is null
            ? Unauthorized(new { error = "invalid_credentials" })
            : Ok(new { status = "authenticated", token = r.Token, expiresAt = r.ExpiresAt, user = r.User });
    }

    // Called by the gateway on every protected request
    [HttpGet("[action]")]
    public async Task<IActionResult> Session()
    {
        var token = Bearer();
        if (token is null) return Unauthorized();
        var s = await auth.GetSessionAsync(token);
        return s is null ? Unauthorized() : Ok(s);
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> Logout()
    {
        var token = Bearer();
        if (token is not null) await auth.LogoutAsync(token);
        return NoContent();
    }
}
