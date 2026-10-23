using System.Text.Json;
using Genkan.Notification.Infrastructure.Interfaces;
using Genkan.Shared.Messaging;
using Genkan.Shared.Messaging.Lab;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Genkan.Notification.Infrastructure.Messaging;

public sealed class RabbitMqLabBus(RabbitMqConnection connection) : ILabBus
{
    public async Task PublishAsync<T>(string routingKey, T message, CancellationToken ct)
    {
        var conn = await connection.GetAsync(ct);
        await using var ch = await conn.CreateChannelAsync(cancellationToken: ct);
        await LabTopology.DeclareAsync(ch, ct); // makes sure the lab queue exists so nothing is dropped

        var props = new BasicProperties { Persistent = true, ContentType = "application/json", MessageId = Guid.NewGuid().ToString() };
        await ch.BasicPublishAsync(LabTopology.Exchange, routingKey, mandatory: false, basicProperties: props,
            body: JsonSerializer.SerializeToUtf8Bytes(message, EventJson.Options), cancellationToken: ct);
    }

    public async Task<DeviceSnapshot> RequestSnapshotAsync(TimeSpan timeout, CancellationToken ct)
    {
        var conn = await connection.GetAsync(ct);
        await using var ch = await conn.CreateChannelAsync(cancellationToken: ct);
        await LabTopology.DeclareAsync(ch, ct);

        // Private reply queue: server-named, deleted with this channel's connection or when the consumer goes away.
        var reply = await ch.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true, cancellationToken: ct);
        var correlationId = Guid.NewGuid().ToString("N");
        var answer = new TaskCompletionSource<DeviceSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);

        var consumer = new AsyncEventingBasicConsumer(ch);
        consumer.ReceivedAsync += (_, ea) =>
        {
            if (ea.BasicProperties.CorrelationId != correlationId) return Task.CompletedTask;
            try
            {
                answer.TrySetResult(JsonSerializer.Deserialize<DeviceSnapshot>(ea.Body.Span, EventJson.Options)
                                    ?? throw new JsonException("Empty snapshot"));
            }
            catch (Exception ex) { answer.TrySetException(ex); }
            return Task.CompletedTask;
        };
        await ch.BasicConsumeAsync(reply.QueueName, autoAck: true, consumer: consumer, cancellationToken: ct);

        var props = new BasicProperties
        {
            ReplyTo = reply.QueueName,
            CorrelationId = correlationId,
            Expiration = ((int)timeout.TotalMilliseconds).ToString(), // a request nobody answered in time must not wait in the lab queue
            ContentType = "application/json",
        };
        await ch.BasicPublishAsync(LabTopology.Exchange, LabTopology.SnapshotRequestKey, mandatory: false, basicProperties: props,
            body: "{}"u8.ToArray(), cancellationToken: ct);

        try { return await answer.Task.WaitAsync(timeout, ct); }
        catch (TimeoutException) { throw new TimeoutException($"The lab service did not answer the snapshot request within {timeout.TotalSeconds:0} s."); }
    }
}
