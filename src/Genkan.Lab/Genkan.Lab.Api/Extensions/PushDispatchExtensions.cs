using Genkan.Lab.Domain.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Genkan.Lab.Api.Extensions;

public static class PushDispatchExtensions
{
    /// <summary>202 when the push was handed to RabbitMQ, 404 when there was no device to send to, 503 when the broker could not be reached.</summary>
    public static IActionResult ToActionResult(this PushDispatch result, ControllerBase controller)
    {
        if (result.Error == PushDispatch.NoDevice) return controller.NotFound(new { error = result.Error });
        if (result.Error is not null) return controller.StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = result.Error });
        return controller.Accepted(result);
    }
}

public static class SubscriptionDispatchExtensions
{
    /// <summary>
    /// For subscribe / unsubscribe: 200 { [countName]: n } when the command was published (n = devices addressed, possibly 0),
    /// 503 when the broker could not be reached. The notification service applies it asynchronously.
    /// </summary>
    public static IActionResult ToActionResult(this PushDispatch result, ControllerBase controller, string countName)
    {
        if (result.Error is not null) return controller.StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = result.Error });
        return controller.Ok(new Dictionary<string, int> { [countName] = result.Targets });
    }
}
