using Genkan.Shared.Messaging.Lab;

namespace Genkan.Lab.Infrastructure.Interfaces;

public interface IDeviceTokenRepository
{
    /// <summary>Registers the token for the user; if the token already exists it is re-assigned to this user.</summary>
    Task UpsertAsync(Guid userId, string token, string platform, CancellationToken ct);
    Task<bool> RemoveAsync(Guid userId, string token, CancellationToken ct);
    /// <summary>Every registered device (the content of a snapshot for the notification service).</summary>
    Task<IReadOnlyList<DeviceSnapshotItem>> GetAllAsync(CancellationToken ct);
    /// <summary>Tokens of several users in one query.</summary>
    Task<IReadOnlyList<string>> GetTokensForUsersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct);
    /// <summary>The most recently used token of the user, or null when none is registered.</summary>
    Task<string?> GetLatestTokenAsync(Guid userId, CancellationToken ct);
    Task RemoveTokensAsync(IReadOnlyCollection<string> tokens, CancellationToken ct);
}
