using Genkan.Lab.Api.Extensions;
using Genkan.Lab.Application.Interfaces;
using Genkan.Lab.Domain.Dto;
using Genkan.Shared.Auth;
using Genkan.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genkan.Lab.Api.Controllers;

[ApiController]
[Route("lab/push/multicast")]
[Authorize(Roles = Roles.Admin)]
public sealed class MulticastPushController(IMulticastPushService push) : BaseController
{
    // { "userIds": ["..."], "tokens": ["..."], "message": { "title": "...", "body": "..." } }
    [HttpPost]
    public async Task<IActionResult> Send(MulticastPushRequest request, CancellationToken ct)
    {
        if (!request.Message.IsValid()) return BadRequest(new { error = "title_and_body_required" });
        if (request.Tokens is not { Count: > 0 } && request.UserIds is not { Count: > 0 })
            return BadRequest(new { error = "tokens_or_users_required" });

        var result = await push.SendAsync(request, ct);
        return result.ToActionResult(this);
    }
}
