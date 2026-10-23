using Genkan.Lab.Api.Extensions;
using Genkan.Lab.Application.Interfaces;
using Genkan.Lab.Domain.Dto;
using Genkan.Shared.Auth;
using Genkan.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genkan.Lab.Api.Controllers;

[ApiController]
[Route("lab/push/groups")]
[Authorize(Roles = Roles.Admin)]
public sealed class DeviceGroupPushController(IDeviceGroupPushService groups) : BaseController
{
    [HttpGet]
    public Task<IReadOnlyList<DeviceGroupDto>> List(CancellationToken ct) => groups.ListAsync(ct);

    // Creates the group or replaces its members: { "userIds": ["..."] }. An empty list deletes the group.
    [HttpPut("{name}")]
    public async Task<IActionResult> SetMembers(string name, UserIdsRequest request, CancellationToken ct)
    {
        if (!PushRules.IsValidGroupName(name)) return BadRequest(new { error = "invalid_group_name" });
        await groups.SetMembersAsync(name, request.UserIds, ct);
        return NoContent();
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name, CancellationToken ct)
    {
        if (!PushRules.IsValidGroupName(name)) return BadRequest(new { error = "invalid_group_name" });
        if (!await groups.DeleteAsync(name, ct)) return NotFound();
        return NoContent();
    }

    // { "title": "...", "body": "...", "data": { } }
    [HttpPost("{name}/send")]
    public async Task<IActionResult> Send(string name, PushMessage message, CancellationToken ct)
    {
        if (!PushRules.IsValidGroupName(name)) return BadRequest(new { error = "invalid_group_name" });
        if (!message.IsValid()) return BadRequest(new { error = "title_and_body_required" });

        var result = await groups.SendAsync(name, message, ct);
        if (result is null) return NotFound(new { error = "group_not_found" });
        return result.ToActionResult(this);
    }
}
