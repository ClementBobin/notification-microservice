using StackExchange.Redis;

namespace Genkan.Gateway.RateLimiting;

/// <summary>
/// Buckets kept in Redis so that every gateway instance shares the same counters. The refill and the take are one Lua script,
/// which Redis runs atomically, and it uses the Redis clock (TIME) so the gateway instances cannot disagree about time.
/// If Redis is unreachable the request is allowed (fail open): a Redis outage must not take the door control down.
/// </summary>
public sealed class RedisRateLimitStore(IConnectionMultiplexer redis, ILogger<RedisRateLimitStore> log) : IRateLimitStore
{
    // KEYS[1] bucket; ARGV[1] capacity, ARGV[2] tokens per second, ARGV[3] cost.
    // Returns { allowed (0/1), whole tokens left, retry-after in ms }.
    internal const string Script = """
        local t = redis.call('TIME')
        local now = tonumber(t[1]) * 1000 + math.floor(tonumber(t[2]) / 1000)
        local capacity = tonumber(ARGV[1])
        local rate = tonumber(ARGV[2])
        local cost = tonumber(ARGV[3])

        local data = redis.call('HMGET', KEYS[1], 'tokens', 'ts')
        local tokens = tonumber(data[1])
        local ts = tonumber(data[2])
        if tokens == nil or ts == nil then
          tokens = capacity
          ts = now
        end

        local elapsed = math.max(0, now - ts)
        tokens = math.min(capacity, tokens + elapsed * rate / 1000)

        local allowed = 0
        local retry = 0
        if tokens >= cost then
          tokens = tokens - cost
          allowed = 1
        else
          retry = math.ceil((cost - tokens) / rate * 1000)
        end

        redis.call('HSET', KEYS[1], 'tokens', tostring(tokens), 'ts', tostring(now))
        redis.call('PEXPIRE', KEYS[1], math.ceil(capacity / rate * 1000) + 5000)
        return { allowed, math.floor(tokens), retry }
        """;

    public async ValueTask<RateLimitDecision> TryAcquireAsync(string key, RateLimitPolicyOptions policy, CancellationToken ct)
    {
        try
        {
            var result = await redis.GetDatabase().ScriptEvaluateAsync(
                Script,
                [(RedisKey)$"ratelimit:{key}"],
                [policy.Capacity, policy.TokensPerSecond, 1]);

            var values = (RedisResult[])result!;
            var allowed = (int)values[0] == 1;
            return new RateLimitDecision(allowed, (int)values[1], TimeSpan.FromMilliseconds((long)values[2]));
        }
        catch (RedisException ex)
        {
            log.LogWarning(ex, "Rate limit store unavailable, allowing the request");
            return new RateLimitDecision(true, policy.Capacity, TimeSpan.Zero);
        }
    }
}
