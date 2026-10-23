using Genkan.Auth.Application.Interfaces;
using Genkan.Auth.Domain.Dto;
using Genkan.Shared.Auth;
using Genkan.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Genkan.Auth.Api.Controllers;

[ApiController]
[Route("user")]
public sealed class UserController(IUserService service) : BaseController
{
    // Admin only
    [HttpPost("users")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreateUser(CreateUserRequest req, CancellationToken ct)
    {
        var (user, error) = await service.CreateUserAsync(req.Email, req.DisplayName, req.Password, req.Role, ct);
        if (error is null) return Created($"/user/{user!.Id}", user);
        return StatusCode(error == "email_taken" ? 409 : 400, new { error });
    }

    /// <summary>
    /// Register a new user FCM token for push notifications.
    /// </summary>
    [HttpPost("fcm-token")]
    [Authorize]
    public async Task<IActionResult> RegisterFcmToken(RegisterFcmTokenRequest req, CancellationToken ct)
    {
        if (req is null) return BadRequest();
        var token = Request.Headers.Authorization.ToString();
        if (!token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return Unauthorized();
        token = token[7..].Trim();
        var error = await service.RegisterFcmTokenAsync(token, req.FcmToken, ct);
        return error is null ? NoContent() : StatusCode(error == "invalid_token" ? 401 : 400, new { error });
    }
}
