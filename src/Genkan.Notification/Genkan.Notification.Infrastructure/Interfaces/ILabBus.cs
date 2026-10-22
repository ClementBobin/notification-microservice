namespace Genkan.Notification.Infrastructure.Interfaces;

/// <summary>Talks to the lab service over RabbitMQ.</summary>
public interface ILabBus
{
    /// <summary>Publishes a persistent JSON message on the lab exchange with this routing key. Throws when the broker cannot be reached.</summary>
    Task PublishAsync<T>(string routingKey, T message, CancellationToken ct);

    /// <summary>Asks the lab service for every device and waits for the answer (RPC over a private reply queue). Throws on timeout.</summary>
    Task<Genkan.Shared.DeviceSnapshot> RequestSnapshotAsync(TimeSpan timeout, CancellationToken ct);
}
