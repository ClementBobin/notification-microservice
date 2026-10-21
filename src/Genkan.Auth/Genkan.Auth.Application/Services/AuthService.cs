using Genkan.Auth.Application.Interfaces;
using Genkan.Auth.Domain.Dto;
using Genkan.Auth.Infrastructure.Interfaces;
using Genkan.Shared;
using Genkan.Shared.Entities;
using Microsoft.Extensions.Options;

namespace Genkan.Auth.Application.Services;

public sealed class AuthService(IUserRepository users, ISessionStore sessions, IOptions<AuthOptions> options) : IAuthService
{
    // Verified against when the e-mail is unknown, to keep response time similar.
    private static readonly string DummyHash = PasswordHasher.Hash("dummy-password-for-timing");
    private TimeSpan SessionTtl => TimeSpan.FromHours(options.Value.SessionHours);

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();

    private static UserDto ToDto(User u) => new(u.Id, u.Email, u.DisplayName, u.Role);

    public async Task<LoginResult?> LoginAsync(string email, string password, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(Normalize(email), ct);
        var ok = PasswordHasher.Verify(password, user?.PasswordHash ?? DummyHash);
        if (user is null || !ok) return null;

        var token = await sessions.CreateSessionAsync(new SessionInfo(user.Id, user.Email, user.Role, user.DisplayName), SessionTtl);
        return new LoginResult(token, DateTimeOffset.UtcNow.Add(SessionTtl), ToDto(user));
    }

    public Task<SessionInfo?> GetSessionAsync(string token) => sessions.GetSessionAsync(token);

    public Task LogoutAsync(string token) => sessions.DeleteSessionAsync(token);

    public async Task<(UserDto? User, string? Error)> CreateUserAsync(string email, string? displayName, string password, string role, CancellationToken ct)
    {
        email = Normalize(email);
        if (!email.Contains('@')) return (null, "invalid_email");
        if (password.Length < 12) return (null, "password_too_short");
        if (!Roles.All.Contains(role)) return (null, "invalid_role");
        if (await users.FindByEmailAsync(email, ct) is not null) return (null, "email_taken");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? email : displayName.Trim(),
            PasswordHash = PasswordHasher.Hash(password),
            Role = role,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await users.AddAsync(user, ct);
        return (ToDto(user), null);
    }
}
