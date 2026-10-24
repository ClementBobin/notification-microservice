using Genkan.Shared.Configuration;
using Genkan.Notification.Domain.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Genkan.Shared.Controllers;

namespace Genkan.Notification.Api.Controllers;

[ApiController]
[Route("notifications/firebase-config")]
public sealed class FirebaseConfigController(IOptions<FirebaseClientOptions> options) : BaseController
{
    // What the app needs to call FirebaseApp.initializeApp(...) at runtime and obtain its FCM token.
    // Public identifiers only; the service-account JSON (private key) is never exposed.
    [HttpGet]
    [Authorize]
    public IActionResult Get()
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.ApiKey) || string.IsNullOrWhiteSpace(o.ApplicationId)
            || string.IsNullOrWhiteSpace(o.MessagingSenderId) || string.IsNullOrWhiteSpace(o.ProjectId))
            return NotFound(new { error = "firebase_client_config_missing" });

        return Ok(new FirebaseClientConfigDto(o.ApiKey, o.ApplicationId, o.MessagingSenderId, o.ProjectId));
    }
}
