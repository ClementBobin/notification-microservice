using System.Text.Json;
using Genkan.Shared;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Genkan.Notification.Messaging;

public sealed class RabbitOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";
}

public sealed class RabbitMqConnection(IOptions<RabbitOptions> options) : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true }) return _connection;
        await _gate.WaitAsync(ct);
        try
        {
            if (_connection is { IsOpen: true }) return _connection;
            var o = options.Value;
            var factory = new ConnectionFactory
            {
                HostName = o.Host,
                Port = o.Port,
                UserName = o.User,
                Password = o.Password,
                AutomaticRecoveryEnabled = true,   // reconnect automatically (non-functional requirement)
                TopologyRecoveryEnabled = true,
            };
            _connection = await factory.CreateConnectionAsync("genkan-notification", ct);
            return _connection;
        }
        finally { _gate.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null) await _connection.DisposeAsync();
    }

    /// <summary>Idempotent: durable topic exchange, durable queue, dead-letter exchange + queue.</summary>
    public static async Task DeclareTopologyAsync(IChannel ch, CancellationToken ct)
    {
        await ch.ExchangeDeclareAsync(Topology.Exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
        await ch.ExchangeDeclareAsync(Topology.DeadLetterExchange, ExchangeType.Fanout, durable: true, cancellationToken: ct);

        await ch.QueueDeclareAsync(Topology.NotificationDeadQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await ch.QueueBindAsync(Topology.NotificationDeadQueue, Topology.DeadLetterExchange, "", cancellationToken: ct);

        var args = new Dictionary<string, object?> { ["x-dead-letter-exchange"] = Topology.DeadLetterExchange };
        await ch.QueueDeclareAsync(Topology.NotificationQueue, durable: true, exclusive: false, autoDelete: false, arguments: args, cancellationToken: ct);
        foreach (var key in Topology.NotificationBindings)
            await ch.QueueBindAsync(Topology.NotificationQueue, Topology.Exchange, key, cancellationToken: ct);
    }
}

public interface IEventPublisher
{
    Task PublishAsync(EventEnvelope envelope, CancellationToken ct);
}

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
