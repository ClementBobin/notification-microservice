using Genkan.Auth.Application.Interfaces;
using Genkan.Auth.Domain.Dto;
using Genkan.Auth.Infrastructure.Interfaces;
using Genkan.Shared;
using Genkan.Shared.Auth;
using Genkan.Shared.Entities;

namespace Genkan.Auth.Application.Services;

public sealed class UserService(IUserRepository users, ISessionStore sessions) : IUserService
{
    private static UserDto ToDto(User u) => new(u.Id, u.Email, u.DisplayName, u.Role);

    public async Task<(UserDto? User, string? Error)> CreateUserAsync(string email, string? displayName, string password, string role, CancellationToken ct)
    {
        email = email.NormalizeEmail();
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

    public async Task<string?> RegisterFcmTokenAsync(string token, string fcmToken, CancellationToken ct)
    {
        var session = await sessions.GetSessionAsync(token);
        if (session is null) return "invalid_token";
        await users.RegisterFcmTokenAsync(session.UserId, fcmToken, ct);
        return null;
    }
}
