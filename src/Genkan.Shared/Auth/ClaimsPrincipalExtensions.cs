using System.Security.Claims;

namespace Genkan.Shared.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The id of the authenticated user (X-User-Id set by the gateway).</summary>
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("No authenticated user."));
}
