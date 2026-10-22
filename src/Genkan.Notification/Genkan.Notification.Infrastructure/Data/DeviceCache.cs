using Genkan.Notification.Infrastructure.Interfaces;
using Genkan.Shared;

namespace Genkan.Notification.Infrastructure.Data;

/// <inheritdoc />
/// <remarks>
/// Live events and snapshots can cross each other, so every entry remembers the lab timestamp of the change that produced it and
/// removals leave a short-lived tombstone: an older message never overrides a newer change, whatever order they arrive in.
/// </remarks>
public sealed class DeviceCache : IDeviceCache
{
    private static readonly TimeSpan TombstoneLifetime = TimeSpan.FromHours(1);

    private sealed record Entry(Guid UserId, string Platform, DateTimeOffset At);

    private readonly object _gate = new();
    private readonly Dictionary<string, Entry> _devices = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DateTimeOffset> _removed = new(StringComparer.Ordinal);
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool IsReady => _ready.Task.IsCompletedSuccessfully;

    public Task WaitUntilReadyAsync(CancellationToken ct) => _ready.Task.WaitAsync(ct);

    public IReadOnlyList<string> GetTokens(Guid? userId)
    {
        lock (_gate)
        {
            return _devices
                .Where(d => userId is null || d.Value.UserId == userId)
                .Select(d => d.Key)
                .ToList();
        }
    }

    public void Upsert(Guid userId, string token, string platform, DateTimeOffset at)
    {
        lock (_gate)
        {
            SetIfNewer(token, new Entry(userId, platform, at));
            Prune(at);
        }
    }

    public void Remove(IEnumerable<string> tokens, DateTimeOffset at)
    {
        lock (_gate)
        {
            foreach (var token in tokens)
            {
                if (_devices.TryGetValue(token, out var current) && current.At > at) continue; // registered again after this removal
                _devices.Remove(token);
                _removed[token] = _removed.TryGetValue(token, out var previous) && previous > at ? previous : at;
            }
            Prune(at);
        }
    }

    public void ApplySnapshot(DeviceSnapshot snapshot)
    {
        lock (_gate)
        {
            var inSnapshot = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in snapshot.Devices)
            {
                inSnapshot.Add(item.Token);
                SetIfNewer(item.Token, new Entry(item.UserId, item.Platform, snapshot.GeneratedAt));
            }

            // Gone from lab's table, and not (re-)registered after the snapshot was taken: remove.
            foreach (var token in _devices.Where(d => !inSnapshot.Contains(d.Key) && d.Value.At < snapshot.GeneratedAt).Select(d => d.Key).ToList())
                _devices.Remove(token);

            Prune(snapshot.GeneratedAt);
        }
        _ready.TrySetResult();
    }

    private void SetIfNewer(string token, Entry entry)
    {
        if (_removed.TryGetValue(token, out var removedAt) && removedAt >= entry.At) return; // removed after this change
        if (_devices.TryGetValue(token, out var current) && current.At > entry.At) return;   // a newer change is already applied
        _devices[token] = entry;
        _removed.Remove(token);
    }

    private void Prune(DateTimeOffset now)
    {
        if (_removed.Count == 0) return;
        foreach (var token in _removed.Where(r => now - r.Value > TombstoneLifetime).Select(r => r.Key).ToList())
            _removed.Remove(token);
    }
}
