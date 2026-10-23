using System.Text.Json;
using Genkan.Notification.Infrastructure.Interfaces;
using Genkan.Shared.Messaging;
using Genkan.Shared.Messaging.Lab;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Genkan.Notification.Infrastructure.Messaging;

public sealed class LabSyncOptions
{
    /// <summary>How often the full device list is requested again from lab, as a safety net for events missed while RabbitMQ or this instance was down. 0 = never.</summary>
    public int SnapshotRefreshMinutes { get; set; } = 5;
    public int SnapshotTimeoutSeconds { get; set; } = 10;
}

/// <summary>
/// Keeps <see cref="IDeviceCache"/> in step with the lab service: it listens to device.registered / device.removed on a private queue
/// (every instance gets every event) and loads a full snapshot at startup and then at every refresh interval.
/// </summary>
public sealed class DeviceSyncService(
    RabbitMqConnection connection, ILabBus lab, IDeviceCache cache, IOptions<LabSyncOptions> options, ILogger<DeviceSyncService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        IChannel? channel = null;
        while (!ct.IsCancellationRequested && channel is null)
        {
            try { channel = await ListenAsync(ct); }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                log.LogWarning("RabbitMQ not ready ({Message}), retrying in 5 s", ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }

        try
        {
            // The listener is already running, so a change made while the snapshot is in flight is not lost (timestamps decide who wins).
            var refresh = options.Value.SnapshotRefreshMinutes;
            while (!ct.IsCancellationRequested)
            {
                var loaded = await TryLoadSnapshotAsync(ct);
                var wait = loaded && refresh > 0 ? TimeSpan.FromMinutes(refresh) : loaded ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(5);
                await Task.Delay(wait, ct);
            }
        }
        catch (OperationCanceledException) { }
        finally { if (channel is not null) await channel.DisposeAsync(); }
    }

    private async Task<bool> TryLoadSnapshotAsync(CancellationToken ct)
    {
        try
        {
            var snapshot = await lab.RequestSnapshotAsync(TimeSpan.FromSeconds(Math.Max(options.Value.SnapshotTimeoutSeconds, 1)), ct);
            cache.ApplySnapshot(snapshot);
            log.LogInformation("Device cache loaded from lab: {Count} device(s)", snapshot.Devices.Count);
            return true;
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            log.LogWarning("Could not load the devices from the lab service ({Message}), retrying", ex.Message);
            return false;
        }
    }

    private async Task<IChannel> ListenAsync(CancellationToken ct)
    {
        var conn = await connection.GetAsync(ct);
        var channel = await conn.CreateChannelAsync(cancellationToken: ct);
        await LabTopology.DeclareAsync(channel, ct);

        // Private queue per instance: every instance must see every device event, so they cannot share one queue.
        var queue = await channel.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true, cancellationToken: ct);
        await channel.QueueBindAsync(queue.QueueName, LabTopology.Exchange, LabTopology.DeviceRegisteredKey, cancellationToken: ct);
        await channel.QueueBindAsync(queue.QueueName, LabTopology.Exchange, LabTopology.DeviceRemovedKey, cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, ea) =>
        {
            try
            {
                switch (ea.RoutingKey)
                {
                    case LabTopology.DeviceRegisteredKey:
                        var added = JsonSerializer.Deserialize<DeviceRegistered>(ea.Body.Span, EventJson.Options) ?? throw new JsonException("Empty message");
                        cache.Upsert(added.UserId, added.Token, added.Platform, added.At);
                        break;
                    case LabTopology.DeviceRemovedKey:
                        var removed = JsonSerializer.Deserialize<DevicesRemoved>(ea.Body.Span, EventJson.Options) ?? throw new JsonException("Empty message");
                        cache.Remove(removed.Tokens, removed.At);
                        break;
                }
            }
            catch (JsonException ex)
            {
                log.LogError(ex, "Ignoring an unreadable device event ({RoutingKey})", ea.RoutingKey);
            }
            return Task.CompletedTask;
        };
        await channel.BasicConsumeAsync(queue.QueueName, autoAck: true, consumer: consumer, cancellationToken: ct);
        log.LogInformation("Listening to device changes from the lab service");
        return channel;
    }
}
