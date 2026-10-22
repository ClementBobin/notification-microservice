namespace Genkan.Lab.Infrastructure.Interfaces;

public interface ITopicSubscriptionRepository
{
    Task AddAsync(string topic, IReadOnlyCollection<Guid> userIds, CancellationToken ct);
    Task RemoveAsync(string topic, IReadOnlyCollection<Guid> userIds, CancellationToken ct);
    Task<IReadOnlyList<string>> GetTopicsAsync(Guid userId, CancellationToken ct);
}
