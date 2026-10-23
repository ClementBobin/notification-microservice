namespace Genkan.Lab.Infrastructure.Interfaces;

public interface IMessageBus
{
    /// <summary>Publishes a persistent JSON message on the lab exchange (<see cref="Genkan.Shared.Messaging.Lab.LabTopology.Exchange"/>) with this routing key. Throws when the broker cannot be reached.</summary>
    Task PublishAsync<T>(string routingKey, T message, CancellationToken ct);

    /// <summary>Answers an RPC request: publishes to the reply queue named in the request's ReplyTo, with the request's correlation id.</summary>
    Task ReplyAsync<T>(string replyTo, string? correlationId, T message, CancellationToken ct);
}
