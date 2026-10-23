using Genkan.Lab.Api.Extensions;
using Genkan.Lab.Application.Interfaces;
using Genkan.Lab.Domain.Dto;
using Genkan.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genkan.Lab.Api.Controllers;

// Subscribing is admin-only on purpose: a user must not be able to subscribe themselves to another building's topic.
[ApiController]
[Route("lab/push/topics")]
[Authorize(Roles = Roles.Admin)]
public sealed class TopicPushController(ITopicPushService topics) : BaseController
{
    // { "title": "...", "body": "...", "data": { } }
    [HttpPost("{topic}/send")]
    public async Task<IActionResult> Send(string topic, PushMessage message, CancellationToken ct)
    {
        if (!PushRules.IsValidTopic(topic)) return BadRequest(new { error = "invalid_topic" });
        if (!message.IsValid()) return BadRequest(new { error = "title_and_body_required" });

        var result = await topics.SendAsync(topic, message, ct);
        return result.ToActionResult(this);
    }

    // { "userIds": ["..."] } : every registered device of those users is subscribed (and re-subscribed when its token changes)
    [HttpPost("{topic}/subscribe")]
    public async Task<IActionResult> Subscribe(string topic, UserIdsRequest request, CancellationToken ct)
    {
        if (!PushRules.IsValidTopic(topic)) return BadRequest(new { error = "invalid_topic" });
        var result = await topics.SubscribeAsync(topic, request.UserIds, ct);
        return result.ToActionResult(this, "subscribedDevices");
    }

    [HttpPost("{topic}/unsubscribe")]
    public async Task<IActionResult> Unsubscribe(string topic, UserIdsRequest request, CancellationToken ct)
    {
        if (!PushRules.IsValidTopic(topic)) return BadRequest(new { error = "invalid_topic" });
        var result = await topics.UnsubscribeAsync(topic, request.UserIds, ct);
        return result.ToActionResult(this, "unsubscribedDevices");
    }

    // Subscribes every device that is already registered to the broadcast topic. New devices are subscribed when they register;
    // this is for the ones registered before that existed. Safe to call again: subscribing twice changes nothing.
    [HttpPost("broadcast/sync")]
    public async Task<IActionResult> SyncBroadcast(CancellationToken ct)
    {
        var result = await topics.SyncBroadcastAsync(ct);
        return result.ToActionResult(this, "subscribedDevices");
    }
}
