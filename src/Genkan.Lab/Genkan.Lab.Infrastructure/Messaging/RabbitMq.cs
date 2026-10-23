using System.Text.Json;
using Genkan.Lab.Infrastructure.Interfaces;
using Genkan.Shared.Messaging;
using Genkan.Shared.Messaging.Lab;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Genkan.Lab.Infrastructure.Messaging;

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
                AutomaticRecoveryEnabled = true,   // reconnect automatically
                TopologyRecoveryEnabled = true,
            };
            _connection = await factory.CreateConnectionAsync("genkan-lab", ct);
            return _connection;
        }
        finally { _gate.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null) await _connection.DisposeAsync();
    }
}

public sealed class RabbitMqMessageBus(RabbitMqConnection connection) : IMessageBus
{
    public Task PublishAsync<T>(string routingKey, T message, CancellationToken ct) =>
        SendAsync(LabTopology.Exchange, routingKey, null, message, ct);

    // The default exchange routes by queue name: the reply goes straight to the queue the requester named in ReplyTo.
    public Task ReplyAsync<T>(string replyTo, string? correlationId, T message, CancellationToken ct) =>
        SendAsync("", replyTo, correlationId, message, ct);

    private async Task SendAsync<T>(string exchange, string routingKey, string? correlationId, T message, CancellationToken ct)
    {
        var conn = await connection.GetAsync(ct);
        await using var ch = await conn.CreateChannelAsync(cancellationToken: ct);
        await LabTopology.DeclareAsync(ch, ct); // makes sure the queues exist so nothing is dropped

        var body = JsonSerializer.SerializeToUtf8Bytes(message, EventJson.Options);
        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString(),
            CorrelationId = correlationId,
        };
        await ch.BasicPublishAsync(exchange, routingKey, mandatory: false, basicProperties: props, body: body, cancellationToken: ct);
    }
}
