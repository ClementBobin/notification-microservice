using Genkan.Auth.Domain.Configuration;
using Genkan.Auth.Application.Interfaces;
using Genkan.Auth.Domain.Dto;
using Genkan.Auth.Infrastructure.Interfaces;
using Genkan.Shared;
using Genkan.Shared.Auth;
using Genkan.Shared.Entities;
using Microsoft.Extensions.Options;

namespace Genkan.Auth.Application.Services;

public sealed class AuthService(IUserRepository users, ISessionStore sessions, IOptions<AuthOptions> options) : IAuthService
{
    // Verified against when the e-mail is unknown, to keep response time similar.
    private static readonly string DummyHash = PasswordHasher.Hash("dummy-password-for-timing");
    private TimeSpan SessionTtl => TimeSpan.FromHours(options.Value.SessionHours);

    private static UserDto ToDto(User u) => new(u.Id, u.Email, u.DisplayName, u.Role);

    public async Task<LoginResult?> LoginAsync(string email, string password, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(email.NormalizeEmail(), ct);
        var ok = PasswordHasher.Verify(password, user?.PasswordHash ?? DummyHash);
        if (user is null || !ok) return null;

        var token = await sessions.CreateSessionAsync(new SessionInfo(user.Id, user.Email, user.Role, user.DisplayName), SessionTtl);
        return new LoginResult(token, DateTimeOffset.UtcNow.Add(SessionTtl), ToDto(user));
    }

    public Task<SessionInfo?> GetSessionAsync(string token) => sessions.GetSessionAsync(token);

    public Task LogoutAsync(string token) => sessions.DeleteSessionAsync(token);
}
