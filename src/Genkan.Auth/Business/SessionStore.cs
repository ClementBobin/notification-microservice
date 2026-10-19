using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using StackExchange.Redis;

namespace Genkan.Auth.Business;

public sealed record SessionInfo(Guid UserId, string Email, string Role, string DisplayName);

public interface ISessionStore
{
    Task<string> CreateSessionAsync(SessionInfo info, TimeSpan ttl);
    Task<SessionInfo?> GetSessionAsync(string token);
    Task DeleteSessionAsync(string token);

    Task<string> CreateChallengeAsync(Guid userId);
    /// <summary>Counts an attempt; returns the user id, or null if the challenge is unknown, expired or exhausted (5 attempts).</summary>
    Task<Guid?> ConsumeChallengeAttemptAsync(string challengeId);
    Task DeleteChallengeAsync(string challengeId);

    Task SetPendingTotpSecretAsync(Guid userId, string secret);
    Task<string?> GetPendingTotpSecretAsync(Guid userId);
    Task DeletePendingTotpSecretAsync(Guid userId);
}

/// <summary>Sessions (opaque tokens) and 2FA challenges, all with expiry, in Redis.</summary>
public sealed class RedisSessionStore(IConnectionMultiplexer redis) : ISessionStore
{
    private const int MaxChallengeAttempts = 5;
    private static readonly TimeSpan ChallengeTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SetupTtl = TimeSpan.FromMinutes(10);
    private IDatabase Db => redis.GetDatabase();

    private static string NewToken() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    public async Task<string> CreateSessionAsync(SessionInfo info, TimeSpan ttl)
    {
        var token = NewToken();
        await Db.StringSetAsync($"session:{token}", JsonSerializer.Serialize(info), ttl);
        return token;
    }

    public async Task<SessionInfo?> GetSessionAsync(string token)
    {
        var json = await Db.StringGetAsync($"session:{token}");
        return json.IsNullOrEmpty ? null : JsonSerializer.Deserialize<SessionInfo>(json.ToString());
    }

    public Task DeleteSessionAsync(string token) => Db.KeyDeleteAsync($"session:{token}");

    public async Task<string> CreateChallengeAsync(Guid userId)
    {
        var id = NewToken();
        var key = $"challenge:{id}";
        await Db.HashSetAsync(key, [new HashEntry("user", userId.ToString()), new HashEntry("attempts", 0)]);
        await Db.KeyExpireAsync(key, ChallengeTtl);
        return id;
    }

    public async Task<Guid?> ConsumeChallengeAttemptAsync(string challengeId)
    {
        var key = $"challenge:{challengeId}";
        var user = await Db.HashGetAsync(key, "user");
        if (user.IsNullOrEmpty) return null;
        var attempts = await Db.HashIncrementAsync(key, "attempts");
        if (attempts > MaxChallengeAttempts)
        {
            await Db.KeyDeleteAsync(key);
            return null;
        }
        return Guid.Parse(user.ToString());
    }

    public Task DeleteChallengeAsync(string challengeId) => Db.KeyDeleteAsync($"challenge:{challengeId}");

    public Task SetPendingTotpSecretAsync(Guid userId, string secret) =>
        Db.StringSetAsync($"totp-setup:{userId}", secret, SetupTtl);

    public async Task<string?> GetPendingTotpSecretAsync(Guid userId)
    {
        var v = await Db.StringGetAsync($"totp-setup:{userId}");
        return v.IsNullOrEmpty ? null : v.ToString();
    }

    public Task DeletePendingTotpSecretAsync(Guid userId) => Db.KeyDeleteAsync($"totp-setup:{userId}");
}
