using Genkan.Lab.Application.Interfaces;
using Genkan.Lab.Domain.Dto;
using Genkan.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genkan.Lab.Api.Controllers;

[ApiController]
[Route("lab/devices")]
[Authorize]
public sealed class DevicesController(IDeviceService devices) : ControllerBase
{
    private static readonly HashSet<string> Platforms = new(StringComparer.OrdinalIgnoreCase) { "android", "ios", "web" };

    // The mobile app registers its FCM token after login (and whenever FCM rotates it)
    [HttpPost]
    public async Task<IActionResult> Register(RegisterDeviceRequest req, CancellationToken ct)
    {
        var token = req.Token?.Trim() ?? "";
        if (token.Length is 0 or > 1024) return BadRequest(new { error = "invalid_token" });
        if (req.Platform is null || !Platforms.Contains(req.Platform)) return BadRequest(new { error = "invalid_platform" });
        await devices.RegisterAsync(User.GetUserId(), token, req.Platform.ToLowerInvariant(), ct);
        return NoContent();
    }

    // Call on logout so the device stops receiving notifications of this user
    [HttpPost("unregister")]
    public async Task<IActionResult> Unregister(UnregisterDeviceRequest req, CancellationToken ct)
    {
        var token = req.Token?.Trim() ?? "";
        if (token.Length is 0 or > 1024) return BadRequest(new { error = "invalid_token" });
        return await devices.UnregisterAsync(User.GetUserId(), token, ct) ? NoContent() : NotFound();
    }
}
