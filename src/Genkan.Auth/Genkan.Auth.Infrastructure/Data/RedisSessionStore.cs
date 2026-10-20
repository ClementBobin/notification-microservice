using System.Security.Cryptography;
using System.Text.Json;
using Genkan.Auth.Domain.Dto;
using Genkan.Auth.Infrastructure.Interfaces;
using Microsoft.AspNetCore.WebUtilities;
using StackExchange.Redis;

namespace Genkan.Auth.Infrastructure.Data;

/// <summary>Sessions (opaque tokens) with expiry, stored in Redis.</summary>
public sealed class RedisSessionStore(IConnectionMultiplexer redis) : ISessionStore
{
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
}
