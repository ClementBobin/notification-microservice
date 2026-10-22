using System.Text.Json;
using Genkan.Notification.Infrastructure.Interfaces;
using Genkan.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Genkan.Notification.Infrastructure.Messaging;

/// <summary>Consumes the push and topic-subscription commands published by the lab service and hands each one to <see cref="IPushCommandHandler"/>.</summary>
public sealed class PushCommandConsumer(RabbitMqConnection connection, IServiceScopeFactory scopes, ILogger<PushCommandConsumer> log) : BackgroundService
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
                var handler = scope.ServiceProvider.GetRequiredService<IPushCommandHandler>();

                switch (ea.RoutingKey)
                {
                    case LabTopology.PushSendKey:
                        await handler.HandleAsync(Read<PushCommand>(ea), ct);
                        break;
                    case LabTopology.TopicSubscriptionKey:
                        await handler.HandleAsync(Read<TopicSubscriptionCommand>(ea), ct);
                        break;
                    default:
                        log.LogWarning("Ignoring command with unexpected routing key {RoutingKey}", ea.RoutingKey);
                        break;
                }
                await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                // Redeliver once, then reject to the dead-letter queue.
                log.LogError(ex, "Failed to process command {RoutingKey} (redelivered: {Redelivered})", ea.RoutingKey, ea.Redelivered);
                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: !ea.Redelivered, ct);
            }
        };
        await channel.BasicConsumeAsync(LabTopology.PushCommandQueue, autoAck: false, consumer: consumer, cancellationToken: ct);
        log.LogInformation("Consuming queue {Queue}", LabTopology.PushCommandQueue);
        return channel;
    }

    private static T Read<T>(BasicDeliverEventArgs ea) =>
        JsonSerializer.Deserialize<T>(ea.Body.Span, EventJson.Options) ?? throw new JsonException("Empty command");
}
