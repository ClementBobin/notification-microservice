using System.Text.Json;
using Genkan.Lab.Infrastructure.Interfaces;
using Genkan.Shared.Messaging;
using Genkan.Shared.Messaging.Lab;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Genkan.Lab.Infrastructure.Messaging;

/// <summary>
/// Consumes the durable lab queue: snapshot requests of the notification service (RPC, answered on the requester's ReplyTo queue)
/// and the device tokens FCM reported as unregistered.
/// </summary>
public sealed class LabConsumer(RabbitMqConnection connection, IServiceScopeFactory scopes, IMessageBus bus, ILogger<LabConsumer> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
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
        await LabTopology.DeclareAsync(channel, ct);
        await channel.BasicQosAsync(0, 10, false, ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                using var scope = scopes.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<IDeviceSyncHandler>();

                switch (ea.RoutingKey)
                {
                    case LabTopology.SnapshotRequestKey:
                        var replyTo = ea.BasicProperties.ReplyTo;
                        if (string.IsNullOrEmpty(replyTo))
                        {
                            log.LogWarning("Snapshot request without ReplyTo, ignored");
                            break;
                        }
                        var snapshot = await handler.BuildSnapshotAsync(ct);
                        await bus.ReplyAsync(replyTo, ea.BasicProperties.CorrelationId, snapshot, ct);
                        log.LogInformation("Sent a snapshot of {Count} device(s) to {ReplyTo}", snapshot.Devices.Count, replyTo);
                        break;

                    case LabTopology.DeviceInvalidKey:
                        var invalid = JsonSerializer.Deserialize<DevicesInvalid>(ea.Body.Span, EventJson.Options)
                                      ?? throw new JsonException("Empty message");
                        await handler.RemoveInvalidAsync(invalid, ct);
                        break;

                    default:
                        log.LogWarning("Ignoring message with unexpected routing key {RoutingKey}", ea.RoutingKey);
                        break;
                }

                await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                // Redeliver once, then reject to the dead-letter queue.
                log.LogError(ex, "Failed to process message {RoutingKey} (redelivered: {Redelivered})", ea.RoutingKey, ea.Redelivered);
                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: !ea.Redelivered, ct);
            }
        };
        await channel.BasicConsumeAsync(LabTopology.LabQueue, autoAck: false, consumer: consumer, cancellationToken: ct);
        log.LogInformation("Consuming queue {Queue}", LabTopology.LabQueue);
        return channel;
    }
}
