using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Genkan.Shared.Auth;

/// <summary>
/// Trusts the X-User-* headers set by the API Gateway (which strips them from incoming client requests).
/// Services using this must only be reachable from the private network.
/// </summary>
public sealed class GatewayHeaderAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string Scheme = "GatewayHeaders";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var id = Request.Headers[GatewayHeaders.UserId].ToString();
        if (!Guid.TryParse(id, out _)) return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, id),
            new(ClaimTypes.Role, Request.Headers[GatewayHeaders.UserRole].ToString()),
            new(ClaimTypes.Email, Request.Headers[GatewayHeaders.UserEmail].ToString()),
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme)));
    }
}
