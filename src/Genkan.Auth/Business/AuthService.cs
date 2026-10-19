using Genkan.Auth.Entities;
using Genkan.Auth.Repository;
using Genkan.Shared;
using Microsoft.Extensions.Options;

namespace Genkan.Auth.Business;

public sealed class AuthOptions
{
    public int SessionHours { get; set; } = 8;
    public string Issuer { get; set; } = "Genkan";
}

public sealed record UserDto(Guid Id, string Email, string DisplayName, string Role, bool TwoFactorEnabled);

public sealed record LoginResult(bool TwoFactorRequired, string? Token, DateTimeOffset? ExpiresAt, string? ChallengeId, UserDto? User);

public sealed class AuthService(IUserRepository users, ISessionStore sessions, IOptions<AuthOptions> options)
{
    // Verified against when the e-mail is unknown, to keep response time similar.
    private static readonly string DummyHash = PasswordHasher.Hash("dummy-password-for-timing");
    private TimeSpan SessionTtl => TimeSpan.FromHours(options.Value.SessionHours);

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();

    private static UserDto ToDto(User u) => new(u.Id, u.Email, u.DisplayName, u.Role, u.TwoFactorEnabled);

    public async Task<LoginResult?> LoginAsync(string email, string password, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(Normalize(email), ct);
        var ok = PasswordHasher.Verify(password, user?.PasswordHash ?? DummyHash);
        if (user is null || !ok) return null;

        if (user.TwoFactorEnabled)
        {
            var challengeId = await sessions.CreateChallengeAsync(user.Id);
            return new LoginResult(true, null, null, challengeId, null);
        }
        return await StartSessionAsync(user);
    }

    public async Task<LoginResult?> VerifyTwoFactorAsync(string challengeId, string code, CancellationToken ct)
    {
        var userId = await sessions.ConsumeChallengeAttemptAsync(challengeId);
        if (userId is null) return null;
        var user = await users.FindByIdAsync(userId.Value, ct);
        if (user?.TotpSecret is null || !Totp.Verify(user.TotpSecret, code)) return null;
        await sessions.DeleteChallengeAsync(challengeId);
        return await StartSessionAsync(user);
    }

    private async Task<LoginResult> StartSessionAsync(User user)
    {
        var token = await sessions.CreateSessionAsync(new SessionInfo(user.Id, user.Email, user.Role, user.DisplayName), SessionTtl);
        return new LoginResult(false, token, DateTimeOffset.UtcNow.Add(SessionTtl), null, ToDto(user));
    }

    public Task<SessionInfo?> GetSessionAsync(string token) => sessions.GetSessionAsync(token);

    public Task LogoutAsync(string token) => sessions.DeleteSessionAsync(token);

    public async Task<(UserDto? User, string? Error)> CreateUserAsync(string email, string displayName, string password, string role, CancellationToken ct)
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

    public async Task<(string Secret, string OtpAuthUri)?> BeginTwoFactorSetupAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId, ct);
        if (user is null || user.TwoFactorEnabled) return null;
        var secret = Totp.NewSecret();
        await sessions.SetPendingTotpSecretAsync(userId, secret);
        return (secret, Totp.BuildUri(options.Value.Issuer, user.Email, secret));
    }

    public async Task<bool> ConfirmTwoFactorAsync(Guid userId, string code, CancellationToken ct)
    {
        var secret = await sessions.GetPendingTotpSecretAsync(userId);
        if (secret is null || !Totp.Verify(secret, code)) return false;
        var user = await users.FindByIdAsync(userId, ct);
        if (user is null) return false;
        user.TotpSecret = secret; // TODO: encrypt at rest (spec section 8)
        user.TwoFactorEnabled = true;
        await users.UpdateAsync(user, ct);
        await sessions.DeletePendingTotpSecretAsync(userId);
        return true;
    }
}
