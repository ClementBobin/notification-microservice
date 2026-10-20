namespace Genkan.Notification.Infrastructure.Interfaces;

public interface IDeviceTokenRepository
{
    /// <summary>Registers the token for the user; if the token already exists it is re-assigned to this user.</summary>
    Task UpsertAsync(Guid userId, string token, string platform, CancellationToken ct);
    Task<bool> RemoveAsync(Guid userId, string token, CancellationToken ct);
    /// <summary>Tokens of one user, or of every user when <paramref name="userId"/> is null (broadcast).</summary>
    Task<IReadOnlyList<string>> GetTokensAsync(Guid? userId, CancellationToken ct);
    Task RemoveTokensAsync(IReadOnlyCollection<string> tokens, CancellationToken ct);
}
