namespace Genkan.Shared.Health;

/// <summary>One named dependency check of a service (database, redis, rabbitmq...).</summary>
public sealed record HealthProbe(string Name, Func<IServiceProvider, CancellationToken, Task<bool>> Check);
