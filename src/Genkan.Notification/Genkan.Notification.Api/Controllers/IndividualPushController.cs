using Genkan.Notification.Api.Extensions;
using Genkan.Notification.Application.Interfaces;
using Genkan.Notification.Domain.Dto;
using Genkan.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genkan.Notification.Api.Controllers;

[ApiController]
[Route("notifications/push/individual")]
[Authorize(Roles = Roles.Admin)]
public sealed class IndividualPushController(IIndividualPushService push) : ControllerBase
{
    // { "userId": "...", "message": { "title": "...", "body": "...", "data": { "doorId": "102" } } }  or  { "token": "..." }
    [HttpPost]
    public async Task<IActionResult> Send(IndividualPushRequest request, CancellationToken ct)
    {
        if (!request.Message.IsValid()) return BadRequest(new { error = "title_and_body_required" });
        if (string.IsNullOrWhiteSpace(request.Token) && request.UserId is null)
            return BadRequest(new { error = "token_or_user_required" });

        var result = await push.SendAsync(request, ct);
        return result.ToActionResult(this);
    }
}
