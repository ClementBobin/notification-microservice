namespace Genkan.Gateway.Configuration;

public enum RateLimitKey
{
    /// <summary>One bucket per authenticated user (X-User-Id set by the gateway); requests without a user fall back to the client IP.</summary>
    User,
    /// <summary>One bucket per client IP.</summary>
    Ip,
}

/// <summary>
/// Token bucket. The bucket holds at most <see cref="Capacity"/> tokens and every request takes one.
/// Tokens come back continuously: <see cref="RefillTokens"/> per <see cref="RefillPeriodSeconds"/>, spread evenly
/// (30 per 60 s = one token every 2 s), instead of all at once when a fixed window resets.
/// </summary>
public sealed class RateLimitPolicyOptions
{
    /// <summary>Bucket size = largest burst a client can send after being idle.</summary>
    public int Capacity { get; set; } = 30;
    public double RefillTokens { get; set; } = 30;
    public double RefillPeriodSeconds { get; set; } = 60;
    public RateLimitKey Key { get; set; } = RateLimitKey.User;

    public double TokensPerSecond => RefillTokens / RefillPeriodSeconds;

    public bool IsValid() => Capacity >= 1 && RefillTokens > 0 && RefillPeriodSeconds > 0;
}

public sealed class RateLimitingOptions
{
    public bool Enabled { get; set; } = true;
    public Dictionary<string, RateLimitPolicyOptions> Policies { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public bool IsValid() => Policies.Values.All(p => p.IsValid());
}
