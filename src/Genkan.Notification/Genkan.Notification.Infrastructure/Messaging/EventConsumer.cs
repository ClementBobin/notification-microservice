using System.Text.Json;
using Genkan.Notification.Infrastructure.Interfaces;
using Genkan.Shared.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Genkan.Notification.Infrastructure.Messaging;

/// <summary>Consumes booking.#, access.#, visitor.# (and alert.#) from the durable queue and hands each event to <see cref="IEventHandler"/>.</summary>
public sealed class EventConsumer(RabbitMqConnection connection, IServiceScopeFactory scopes, IDeviceCache devices, ILogger<EventConsumer> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Events are durable and wait in the queue. Consuming before the devices are known would mark every notification "no_registered_device".
        if (!devices.IsReady) log.LogInformation("Waiting for the device list from the lab service before consuming events");
        try { await devices.WaitUntilReadyAsync(ct); }
        catch (OperationCanceledException) { return; }

        IChannel? channel = null;
        while (!ct.IsCancellationRequested && channel is null)
        {
            try { channel = await StartAsync(ct); }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                log.LogWarning("RabbitMQ not ready ({Message}), retrying in 5 s", ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }

        try { await Task.Delay(Timeout.Infinite, ct); }
        catch (OperationCanceledException) { }
        finally { if (channel is not null) await channel.DisposeAsync(); }
    }

    private async Task<IChannel> StartAsync(CancellationToken ct)
    {
        var conn = await connection.GetAsync(ct);
        var channel = await conn.CreateChannelAsync(cancellationToken: ct);
        await RabbitMqConnection.DeclareTopologyAsync(channel, ct);
        await channel.BasicQosAsync(0, 10, false, ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var envelope = JsonSerializer.Deserialize<EventEnvelope>(ea.Body.Span, EventJson.Options)
                               ?? throw new JsonException("Empty event");
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IEventHandler>().HandleAsync(envelope, ct);
                await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                // Redeliver once, then reject to the dead-letter queue.
                log.LogError(ex, "Failed to process message {RoutingKey} (redelivered: {Redelivered})", ea.RoutingKey, ea.Redelivered);
                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: !ea.Redelivered, ct);
            }
        };
        await channel.BasicConsumeAsync(Topology.NotificationQueue, autoAck: false, consumer: consumer, cancellationToken: ct);
        log.LogInformation("Consuming queue {Queue}", Topology.NotificationQueue);
        return channel;
    }
}
