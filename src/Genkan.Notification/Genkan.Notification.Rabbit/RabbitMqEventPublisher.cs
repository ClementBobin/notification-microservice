using System.Text.Json;
using Genkan.Notification.Application.Interfaces;
using Genkan.Shared.Messaging;
using RabbitMQ.Client;

namespace Genkan.Notification.Rabbit;

public sealed class RabbitMqEventPublisher(RabbitMqConnection connection) : IEventPublisher
{
    public async Task PublishAsync(EventEnvelope envelope, CancellationToken ct)
    {
        var conn = await connection.GetAsync(ct);
        await using var ch = await conn.CreateChannelAsync(cancellationToken: ct);
        await RabbitMqConnection.DeclareTopologyAsync(ch, ct); // makes sure the queue exists so nothing is dropped

        var body = JsonSerializer.SerializeToUtf8Bytes(envelope, EventJson.Options);
        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = envelope.EventId.ToString(),
        };
        await ch.BasicPublishAsync(Topology.Exchange, envelope.Type, mandatory: false, basicProperties: props, body: body, cancellationToken: ct);
    }
}
