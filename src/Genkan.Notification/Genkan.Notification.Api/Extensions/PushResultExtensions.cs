using Genkan.Notification.Domain.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Genkan.Notification.Api.Extensions;

public static class PushResultExtensions
{
    /// <summary>200 when FCM accepted something, 404 when there was no device to send to, 502 when FCM accepted nothing.</summary>
    public static IActionResult ToActionResult(this PushResult result, ControllerBase controller)
    {
        if (result.Error == PushResult.NoDevice) return controller.NotFound(new { error = result.Error });
        if (result.Accepted == 0) return controller.StatusCode(StatusCodes.Status502BadGateway, result);
        return controller.Ok(result);
    }
}
