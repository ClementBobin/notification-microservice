using Genkan.Notification.Api.Extensions;
using Genkan.Notification.Application.Interfaces;
using Genkan.Notification.Domain.Dto;
using Genkan.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Genkan.Notification.Api.Controllers;

// Subscribing is admin-only on purpose: a user must not be able to subscribe themselves to another building's topic.
[ApiController]
[Route("notifications/push/topics")]
[Authorize(Roles = Roles.Admin)]
public sealed class TopicPushController(ITopicPushService topics) : ControllerBase
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
        var devices = await topics.SubscribeAsync(topic, request.UserIds, ct);
        return Ok(new { subscribedDevices = devices });
    }

    [HttpPost("{topic}/unsubscribe")]
    public async Task<IActionResult> Unsubscribe(string topic, UserIdsRequest request, CancellationToken ct)
    {
        if (!PushRules.IsValidTopic(topic)) return BadRequest(new { error = "invalid_topic" });
        var devices = await topics.UnsubscribeAsync(topic, request.UserIds, ct);
        return Ok(new { unsubscribedDevices = devices });
    }
}
